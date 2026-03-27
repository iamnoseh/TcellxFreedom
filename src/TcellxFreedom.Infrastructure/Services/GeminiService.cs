using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TcellxFreedom.Application.DTOs.Gemini;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Infrastructure.Configuration;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class GeminiService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiSettings> options,
    ILogger<GeminiService> logger)
    : IGeminiService
{
    private readonly GeminiSettings _settings = options.Value;
    private readonly ILogger<GeminiService> _logger = logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<GeminiScheduleResult> ScheduleTasksAsync(GeminiScheduleRequest request, CancellationToken ct = default)
    {
        try
        {
            var prompt = BuildSchedulePrompt(request);
            var responseText = await CallGeminiAsync(prompt, ct);
            return ParseScheduleResult(responseText);
        }
        catch
        {
            return BuildFallbackResult(request);
        }
    }

    private static GeminiScheduleResult BuildFallbackResult(GeminiScheduleRequest request)
    {
        var totalDays = (request.EndDate - request.StartDate).Days;
        if (totalDays < 1) totalDays = 1;

        var scheduled = new List<GeminiScheduledTask>();
        var tasks = request.UserTasks;

        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var dayOffset = (int)Math.Round((double)i / Math.Max(tasks.Count - 1, 1) * (totalDays - 1));
            var hour = task.PreferredTimeOfDay?.ToLower() switch
            {
                "morning"   => 9,
                "afternoon" => 14,
                "evening"   => 19,
                _           => 10
            };
            var scheduledAt = ToUtcFromLocal(request.StartDate.AddDays(dayOffset), hour, 0, request.UserTimeZone);

            scheduled.Add(new GeminiScheduledTask(
                task.Title,
                task.Description,
                scheduledAt,
                EstimatedMinutes: 60,
                Rationale: "Задача запланирована согласно приоритету и срокам плана.",
                task.Recurrence));
        }

        return new GeminiScheduleResult(scheduled, []);
    }

    public async Task<GeminiChatScheduleResult> ParseAndScheduleFromChatAsync(GeminiChatScheduleRequest request, CancellationToken ct = default)
    {
        try
        {
            var prompt = BuildChatSchedulePrompt(request);
            var responseText = await CallGeminiAsync(prompt, ct);
            _logger.LogInformation("Gemini raw response (first 800 chars): {Response}",
                responseText.Length > 800 ? responseText[..800] : responseText);
            var result = ParseChatScheduleResult(responseText, request.Date);
            _logger.LogInformation("Gemini parsed: {TaskCount} scheduledTasks, planTitle={Title}",
                result.ScheduledTasks.Count, result.PlanTitle);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini ParseAndScheduleFromChatAsync FAILED — using fallback. Error: {Message}", ex.Message);
            return BuildChatFallbackResult(request);
        }
    }

    public async Task<List<string>> GenerateImprovementSuggestionsAsync(GeminiStatsRequest request, CancellationToken ct = default)
    {
        var prompt = BuildStatsPrompt(request);
        var responseText = await CallGeminiAsync(prompt, ct);
        try
        {
            return JsonSerializer.Deserialize<List<string>>(responseText, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static readonly int[] _retryDelaysSeconds = [30, 60, 90];

    // Serialise all Gemini calls to stay within the free-tier 15 RPM limit.
    // One request at a time + minimum 5 s gap = max 12 RPM.
    private static readonly SemaphoreSlim _geminiGate = new(1, 1);
    private static DateTime _lastGeminiCall = DateTime.MinValue;
    private const int MinInterRequestMs = 5_000;

    private async Task<string> CallGeminiAsync(object requestBody, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Gemini");
        var url = $"/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";
        var json = JsonSerializer.Serialize(requestBody); // immutable string — safe to reuse

        const int maxAttempts = 4;
        const int geminiTimeoutSeconds = 120;
        Exception? lastEx = null;

        await _geminiGate.WaitAsync(CancellationToken.None);
        try
        {

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Enforce minimum gap between requests to avoid 429s proactively.
            var elapsed = (DateTime.UtcNow - _lastGeminiCall).TotalMilliseconds;
            if (elapsed < MinInterRequestMs)
                await Task.Delay(TimeSpan.FromMilliseconds(MinInterRequestMs - elapsed), CancellationToken.None);

            // Use an independent timeout for Gemini — do NOT propagate the HTTP request's
            // CancellationToken directly, because the ASP.NET request can be cancelled
            // (e.g. client disconnect or short pipeline timeout) before Gemini responds.
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(geminiTimeoutSeconds));
            using var linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token);
            var geminict = linkedCts.Token;

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                _lastGeminiCall = DateTime.UtcNow;
                var response = await client.SendAsync(req, geminict);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    throw new InvalidOperationException("Gemini 403: quota exhausted or invalid API key.");

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
                    _logger.LogWarning("Gemini 429 body: {Body}", errorBody);

                    if (attempt >= maxAttempts - 1)
                        throw new InvalidOperationException("Gemini rate limit exceeded after all retry attempts.");

                    var delay = _retryDelaysSeconds[Math.Min(attempt, _retryDelaysSeconds.Length - 1)];

                    // Prefer retryDelay from JSON body (e.g. "59s"), fallback to Retry-After header
                    try
                    {
                        var bodyDoc = JsonNode.Parse(errorBody);
                        var retryDelayStr = bodyDoc?["error"]?["details"]?.AsArray()
                            .Select(d => d?["retryDelay"]?.GetValue<string>())
                            .FirstOrDefault(s => s != null);
                        if (retryDelayStr is not null
                            && int.TryParse(retryDelayStr.TrimEnd('s'), out var bodyDelay)
                            && bodyDelay > 0)
                            delay = Math.Max(delay, bodyDelay);
                    }
                    catch { /* ignore parse errors, use default */ }

                    if (response.Headers.TryGetValues("Retry-After", out var vals)
                        && int.TryParse(vals.FirstOrDefault(), out var headerDelay) && headerDelay > 0)
                        delay = Math.Max(delay, headerDelay);

                    _logger.LogWarning("Gemini 429 on attempt {Attempt}. Waiting {Delay}s before retry.", attempt, delay);
                    await Task.Delay(TimeSpan.FromSeconds(delay), CancellationToken.None);
                    continue;
                }

                // 5xx retries are handled by the Polly resilience handler on the HttpClient.

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
                    _logger.LogError("Gemini {Status} body: {Body}", (int)response.StatusCode, errorBody);
                    response.EnsureSuccessStatusCode();
                }
                var body = await response.Content.ReadAsStringAsync(geminict);
                var doc = JsonNode.Parse(body);
                var text = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
                return text ?? throw new InvalidOperationException("Empty response from Gemini.");
            }
            catch (InvalidOperationException) { throw; }
            catch (TaskCanceledException ex)
            {
                // Propagate only if the original request was cancelled (e.g. user navigated away).
                // If it was our own Gemini timeout, treat it as a retryable failure.
                if (ct.IsCancellationRequested) throw;
                lastEx = ex;
                _logger.LogWarning("Gemini timeout on attempt {Attempt}/{Max}.", attempt + 1, maxAttempts);
            }
            catch (Exception ex) { lastEx = ex; }

            if (attempt < maxAttempts - 1)
                await Task.Delay(TimeSpan.FromSeconds(_retryDelaysSeconds[Math.Min(attempt, 2)]), CancellationToken.None);
        }

        throw new InvalidOperationException("AI service temporarily unavailable.", lastEx);

        } // end semaphore try
        finally { _geminiGate.Release(); }
    }

    private static object BuildSchedulePrompt(GeminiScheduleRequest request)
    {
        var taskList = request.UserTasks.Select(t => new
        {
            title = t.Title,
            description = t.Description,
            preferredTimeOfDay = t.PreferredTimeOfDay,
            recurrence = t.Recurrence.ToString()
        }).ToList();

        var userMessage = JsonSerializer.Serialize(new
        {
            instruction = "Расставь следующие задачи в указанный период. Назначь точное время для каждой задачи (на основе типа и приоритета). Укажи расчётное время выполнения в минутах. Предложи 2-4 дополнительные полезные задачи. Название, описание и обоснование должны быть на русском языке. Все времена должны быть в формате UTC ISO8601.",
            planRange = new
            {
                startDate = request.StartDate.ToString("yyyy-MM-dd"),
                endDate = request.EndDate.ToString("yyyy-MM-dd"),
                userTimeZone = request.UserTimeZone
            },
            userTasks = taskList,
            responseSchema = new
            {
                scheduledTasks = new[]
                {
                    new { title = "строка (на русском)", description = "строка или null (на русском)", scheduledAtUtc = "ISO8601 datetime", estimatedMinutes = "целое число", rationale = "строка или null (на русском)", recurrence = "None или Daily или Weekly" }
                },
                suggestedAdditionalTasks = new[]
                {
                    new { title = "строка (на русском)", description = "строка или null (на русском)", scheduledAtUtc = "ISO8601 datetime", estimatedMinutes = "целое число", rationale = "строка — обязательно, напишите причину предложения на русском языке" }
                }
            }
        });

        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = "Ты эксперт по личному планированию и управлению временем. Твоя задача — оптимально распределить задачи пользователя в дневном расписании и предложить дополнительные полезные задачи. Все тексты (title, description, rationale) должны быть на РУССКОМ языке. Возвращай только корректный JSON, без внешних пояснений." } }
            },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };
    }

    private static object BuildStatsPrompt(GeminiStatsRequest request)
    {
        var statsJson = JsonSerializer.Serialize(request.RecentWeeks.Select(w => new
        {
            weekStart = w.WeekStart.ToString("yyyy-MM-dd"),
            completionRate = w.CompletionRate,
            totalTasks = w.TotalTasks,
            completedTasks = w.CompletedTasks
        }));

        var userMessage = JsonSerializer.Serialize(new
        {
            instruction = "Проанализируй статистику выполнения задач пользователя и предложи 3-5 конкретных практических рекомендаций для улучшения результатов. Верни ответ в виде JSON-массива строк. Все рекомендации должны быть на РУССКОМ языке.",
            weeklyStats = JsonSerializer.Deserialize<object>(statsJson)
        });

        return new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };
    }

    private static object BuildChatSchedulePrompt(GeminiChatScheduleRequest request)
    {
        bool isMultiDay = request.EndDate.HasValue && request.EndDate.Value.Date > request.Date.Date;

        // Compute UTC offset for unambiguous timezone conversion
        TimeSpan utcOffset;
        string utcOffsetStr;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(request.UserTimeZone);
            utcOffset = tz.GetUtcOffset(request.Date);
            utcOffsetStr = utcOffset >= TimeSpan.Zero
                ? $"+{utcOffset.Hours:00}:{utcOffset.Minutes:00}"
                : $"-{Math.Abs(utcOffset.Hours):00}:{Math.Abs(utcOffset.Minutes):00}";
        }
        catch { utcOffset = TimeSpan.Zero; utcOffsetStr = "+00:00"; }

        var totalDays = isMultiDay
            ? (int)(request.EndDate!.Value.Date - request.Date.Date).TotalDays + 1
            : 1;

        string instruction;
        if (isMultiDay)
        {
            // Build explicit per-day date list so AI has no ambiguity
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"You are an expert personal coach and curriculum designer. The user wants a {totalDays}-day structured learning/training plan.");
            sb.AppendLine();
            sb.AppendLine("CRITICAL RULES:");
            sb.AppendLine($"1. You MUST generate EXACTLY {totalDays} tasks in scheduledTasks — one unique task per day.");
            sb.AppendLine("2. Each task MUST have a DIFFERENT, SPECIFIC topic/title — never repeat the same title.");
            sb.AppendLine("3. Design a progressive curriculum: start with basics, build up day by day, like a real teacher would.");
            sb.AppendLine($"4. TIME ZONE: User is in UTC{utcOffsetStr}. Convert all times to UTC before writing scheduledAtUtc.");
            sb.AppendLine($"   Example: if user says '22:00' local time → subtract {utcOffsetStr} → write that as UTC in ISO8601 with Z suffix.");
            sb.AppendLine("5. All title, description, planTitle, planDescription fields MUST be in RUSSIAN language.");
            sb.AppendLine("6. suggestedAdditionalTasks MUST be an empty array [].");
            sb.AppendLine();
            sb.AppendLine("Day schedule (use these exact dates):");
            for (int d = 0; d < totalDays; d++)
            {
                var dayDate = request.Date.AddDays(d);
                sb.AppendLine($"  Day {d + 1}: {dayDate:yyyy-MM-dd}");
            }
            sb.AppendLine();
            sb.AppendLine("Example of what GOOD output looks like for a Python plan (titles must be in Russian):");
            sb.AppendLine("  Day 1: 'Python: Установка и настройка рабочей среды (VSCode, Python 3)' — basic setup");
            sb.AppendLine("  Day 2: 'Python: Переменные и типы данных' — variables & data types");
            sb.AppendLine("  Day 3: 'Python: Условные операторы (if/elif/else)' — conditionals");
            sb.AppendLine("  Day 4: 'Python: Циклы (for, while)' — loops");
            sb.AppendLine("  Day 5: 'Python: Списки и кортежи (list, tuple)' — collections");
            sb.AppendLine("  ... and so on, progressively more advanced each day.");
            instruction = sb.ToString();
        }
        else
        {
            instruction = $"You are a personal productivity coach. Extract tasks and times from the user's message. " +
                $"TIME ZONE: User is in UTC{utcOffsetStr}. Convert all times to UTC (ISO8601 with Z). " +
                $"All title/description/planTitle/planDescription must be in RUSSIAN language. " +
                $"Set suggestedAdditionalTasks to empty array [].";
        }

        const int MaxFreeTextChars = 2000;
        var freeText = request.FreeText?.Length > MaxFreeTextChars
            ? request.FreeText[..MaxFreeTextChars]
            : request.FreeText;

        var userMessage = JsonSerializer.Serialize(new
        {
            instruction,
            date = request.Date.ToString("yyyy-MM-dd"),
            endDate = request.EndDate?.ToString("yyyy-MM-dd"),
            totalDays,
            userTimeZone = request.UserTimeZone,
            utcOffset = utcOffsetStr,
            userText = freeText,
            responseSchema = new
            {
                planTitle = "string (Russian)",
                planDescription = "string (Russian)",
                scheduledTasks = new[]
                {
                    new { title = "string (Russian, unique per day)", description = "string (Russian) or null", scheduledAtUtc = "2026-03-24T17:00:00Z", estimatedMinutes = 90, rationale = "string (Russian) or null", recurrence = "None" }
                },
                suggestedAdditionalTasks = Array.Empty<object>()
            }
        });

        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = "You are an expert curriculum designer and personal coach. When building multi-day plans, you MUST produce one unique, progressively harder task per day — like a real teacher designing a course. Task titles and descriptions must be in RUSSIAN language. Return ONLY valid JSON, no extra text." } }
            },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new { responseMimeType = "application/json", maxOutputTokens = 8192 }
        };
    }

    private static GeminiChatScheduleResult ParseChatScheduleResult(string json, DateTime date)
    {
        try
        {
            var doc = JsonNode.Parse(json);
            var planTitle = doc?["planTitle"]?.GetValue<string>() ?? "Ежедневный план";
            var planDescription = doc?["planDescription"]?.GetValue<string>();

            var scheduledTasks = new List<GeminiScheduledTask>();
            var suggestedTasks = new List<GeminiSuggestedTask>();

            var scheduled = doc?["scheduledTasks"]?.AsArray();
            if (scheduled is not null)
            {
                foreach (var item in scheduled)
                {
                    var title = item?["title"]?.GetValue<string>() ?? string.Empty;
                    var description = item?["description"]?.GetValue<string>();
                    var scheduledAtStr = item?["scheduledAtUtc"]?.GetValue<string>() ?? string.Empty;
                    var estimatedMinutes = item?["estimatedMinutes"]?.GetValue<int>() ?? 30;
                    var rationale = item?["rationale"]?.GetValue<string>();
                    var recurrenceStr = item?["recurrence"]?.GetValue<string>() ?? "None";

                    if (DateTime.TryParse(scheduledAtStr, out var scheduledAt))
                    {
                        Enum.TryParse<RecurrenceType>(recurrenceStr, true, out var recurrence);
                        scheduledTasks.Add(new GeminiScheduledTask(title, description, scheduledAt.ToUniversalTime(), estimatedMinutes, rationale, recurrence));
                    }
                }
            }

            var suggested = doc?["suggestedAdditionalTasks"]?.AsArray();
            if (suggested is not null)
            {
                foreach (var item in suggested)
                {
                    var title = item?["title"]?.GetValue<string>() ?? string.Empty;
                    var description = item?["description"]?.GetValue<string>();
                    var scheduledAtStr = item?["scheduledAtUtc"]?.GetValue<string>() ?? string.Empty;
                    var estimatedMinutes = item?["estimatedMinutes"]?.GetValue<int>() ?? 30;
                    var rationale = item?["rationale"]?.GetValue<string>() ?? string.Empty;

                    if (DateTime.TryParse(scheduledAtStr, out var scheduledAt))
                        suggestedTasks.Add(new GeminiSuggestedTask(title, description, scheduledAt.ToUniversalTime(), estimatedMinutes, rationale));
                }
            }

            return new GeminiChatScheduleResult(planTitle, planDescription, scheduledTasks, suggestedTasks);
        }
        catch
        {
            return new GeminiChatScheduleResult("Ежедневный план", null, [], []);
        }
    }

    private static GeminiChatScheduleResult BuildChatFallbackResult(GeminiChatScheduleRequest request)
    {
        if (request.EndDate.HasValue && request.EndDate.Value.Date > request.Date.Date)
            return BuildMultiDayCurriculumFallback(request);

        var tasks = ParseTasksFromText(request.FreeText, request.Date, request.UserTimeZone);
        return new GeminiChatScheduleResult("Ежедневный план", "План составлен на основе текста пользователя.", tasks, []);
    }

    // ─── Smart multi-day fallback ─────────────────────────────────────────────
    // Called when Gemini is unavailable. Generates a unique topic per day based
    // on the subject detected in the user's free text.
    private static GeminiChatScheduleResult BuildMultiDayCurriculumFallback(GeminiChatScheduleRequest request)
    {
        var (startH, startM, durationMins) = ExtractTimeRangeFromText(request.FreeText);
        var (topicKey, topicLabel) = DetectTopic(request.FreeText);
        var curriculum = GetCurriculum(topicKey, topicLabel);

        var totalDays = (int)(request.EndDate!.Value.Date - request.Date.Date).TotalDays + 1;
        var tasks = new List<GeminiScheduledTask>();

        for (int day = 0; day < totalDays; day++)
        {
            var date = request.Date.AddDays(day);
            var scheduledAt = ToUtcFromLocal(date, startH, startM, request.UserTimeZone);
            var title = day < curriculum.Count
                ? curriculum[day]
                : $"{topicLabel}: Урок {day + 1}";

            tasks.Add(new GeminiScheduledTask(
                title,
                $"День {day + 1} из {totalDays}: {title}",
                scheduledAt,
                durationMins,
                null,
                RecurrenceType.None));
        }

        return new GeminiChatScheduleResult(
            $"Курс {topicLabel} ({totalDays} дней)",
            $"{totalDays}-дневный план обучения {topicLabel}",
            tasks,
            []);
    }

    private static (int startH, int startM, int durationMins) ExtractTimeRangeFromText(string text)
    {
        // Match "22:00 - 23:30" or "22:00-23:30" or "22:00"
        var m = Regex.Match(text, @"(\d{1,2}):(\d{2})\s*[-–]\s*(\d{1,2}):(\d{2})");
        if (m.Success)
        {
            int sh = int.Parse(m.Groups[1].Value), sm = int.Parse(m.Groups[2].Value);
            int eh = int.Parse(m.Groups[3].Value), em = int.Parse(m.Groups[4].Value);
            int dur = eh * 60 + em - (sh * 60 + sm);
            return (sh, sm, dur > 0 ? dur : 60);
        }
        var m2 = Regex.Match(text, @"(\d{1,2}):(\d{2})");
        if (m2.Success)
            return (int.Parse(m2.Groups[1].Value), int.Parse(m2.Groups[2].Value), 60);
        return (9, 0, 60);
    }

    private static (string key, string label) DetectTopic(string text)
    {
        var t = text.ToLowerInvariant();
        if (t.Contains("python"))                                      return ("python",      "Python");
        if (t.Contains("javascript") || t.Contains(" js "))           return ("javascript",  "JavaScript");
        if (t.Contains("c#") || t.Contains("csharp"))                 return ("csharp",      "C#");
        if (t.Contains("flutter") || t.Contains("dart"))              return ("flutter",     "Flutter/Dart");
        if (t.Contains("react"))                                       return ("react",       "React");
        if (t.Contains("sql"))                                         return ("sql",         "SQL");
        if (t.Contains("arabic") || t.Contains("арабский"))              return ("arabic",      "Арабский язык");
        if (t.Contains("english") || t.Contains("англис") ||
            t.Contains("инглис") || t.Contains("английск"))           return ("english",     "Английский язык");
        if (t.Contains("бизнес") || t.Contains("business") ||
            t.Contains("стартап") || t.Contains("startup"))           return ("business",    "Бизнес");
        if (t.Contains("экзамен") || t.Contains("exam") ||
            t.Contains("тест") || t.Contains("test") ||
            t.Contains("подготов"))                                    return ("exam",        "Подготовка к экзамену");
        // Fitness: weight loss, exercise, nutrition keywords
        if (t.Contains("варзиш") || t.Contains("sport") || t.Contains("фитнес") ||
            t.Contains("машк") || t.Contains("вазн") || t.Contains("хурок") ||
            t.Contains("диет") || t.Contains("калори") || t.Contains("мушак") ||
            t.Contains("кг") || t.Contains("фарбех") || t.Contains("давид"))
                                                                       return ("fitness",     "Спорт и питание");
        if (t.Contains("математик") || t.Contains("math"))            return ("math",        "Математика");
        // Generic fallback
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('.', ',', '!', '?', ':'))
            .Where(w => w.Length > 4)
            .ToList();
        var label = words.Count > 0 ? words[0] : "Обучение";
        return ("generic", label);
    }

    private static List<string> GetCurriculum(string key, string label) => key switch
    {
        "python" =>
        [
            "Python: Установка и настройка рабочей среды (Python + VSCode)",
            "Python: Переменные и типы данных (int, str, float, bool)",
            "Python: Арифметические и сравнительные операторы",
            "Python: Строки (str) и операции с ними",
            "Python: Условие if / elif / else",
            "Python: Цикл for и range()",
            "Python: Цикл while, break и continue",
            "Python: Функции — def и return",
            "Python: Аргументы функции (*args, **kwargs)",
            "Python: Списки (list) и операции с ними",
            "Python: Кортежи (tuple) и множества (set)",
            "Python: Словари (dict) — ключ и значение",
            "Python: List comprehension и dict comprehension",
            "Python: Работа с файлами (open, read, write)",
            "Python: Исключения — try / except / finally",
            "Python: Модули и import (os, sys, math)",
            "Python: Объектно-ориентированное программирование — class и object",
            "Python: Наследование (inheritance) и __init__",
            "Python: Декораторы и @property",
            "Python: Генераторы (generators) и yield",
            "Python: Регулярные выражения (re)",
            "Python: Работа с датой и временем (datetime, timedelta)",
            "Python: JSON и CSV — чтение и запись",
            "Python: Тесты — pytest и unittest",
            "Python: NumPy — основы числовых вычислений",
            "Python: Pandas — анализ данных",
            "Python: Matplotlib — визуальные графики",
            "Python: Requests — работа с внешними API",
            "Python: Flask — создание простого веб-API",
            "Python: Итоговый проект — применение всех знаний",
        ],
        "javascript" =>
        [
            "JS: Основы — let, const, var и типы",
            "JS: Операторы и строки (template literals)",
            "JS: Условия (if/else) и switch",
            "JS: Циклы — for, while, forEach",
            "JS: Функции и стрелочные функции",
            "JS: Массивы (Array) и операции",
            "JS: Объекты (Object) и деструктуризация",
            "JS: Map, Filter, Reduce",
            "JS: DOM — доступ и изменение",
            "JS: События (Events) и addEventListener",
            "JS: Promise и async/await",
            "JS: Fetch API — работа с серверами",
            "JS: ES6+ — spread, rest, optional chaining",
            "JS: Модули — import/export",
            "JS: Классы (Class) и OOP",
            "JS: localStorage и sessionStorage",
            "JS: Обработка ошибок — try/catch",
            "JS: Regular Expressions",
            "JS: Замыкания и область видимости",
            "JS: Prototype и цепочка прототипов",
            "JS: TypeScript — основы",
            "JS: NPM и package.json",
            "JS: Node.js — простой сервер",
            "JS: Express.js — создание API",
            "JS: React — основы JSX и компонентов",
            "JS: React — useState и useEffect",
            "JS: React — props и children",
            "JS: React — React Router",
            "JS: Написание тестов — Jest",
            "JS: Итоговый проект — Full-stack mini app",
        ],
        "fitness" =>
        [
            "Неделя 1, День 1 — Тренировка: Разминка + 30 минут лёгкого бега",
            "Неделя 1, День 1 — Питание: Завтрак овсянка + вода, обед курица + овощи, ужин рыба + салат",
            "Неделя 1, День 2 — Тренировка: Push-up 3×15, Plank 3×30сек, Squat 3×20",
            "Неделя 1, День 2 — Питание: Калории 1600-1800 ккал, больше белка, меньше сахара",
            "Неделя 1, День 3 — Тренировка: Кардио 40 минут (бег или велосипед)",
            "Неделя 1, День 3 — Питание: Завтрак яйца + тёмный хлеб, обед мясо + коричневый рис",
            "Неделя 1, День 4 — Тренировка: Отдых + Йога для гибкости (15 минут)",
            "Неделя 2, День 1 — Тренировка: HIIT 20 минут — Burpee, Mountain climber, Jump squat",
            "Неделя 2, День 1 — Питание: Много воды (2-2.5л/день), фрукты вместо сладкого",
            "Неделя 2, День 2 — Тренировка: Грудь и плечи — Dumbbell press, Lateral raise",
            "Неделя 2, День 2 — Питание: Завтрак зелёный чай + яйца, ужин не есть после 20:00",
            "Неделя 2, День 3 — Тренировка: Ноги — Lunges 3×15, Deadlift с весом тела",
            "Неделя 2, День 3 — Питание: Белок 1.5г/кг веса — курица, бобы, яйца, рыба",
            "Неделя 2, День 4 — Тренировка: Спина и core — Plank variations, Superman, Bird-dog",
            "Неделя 3, День 1 — Тренировка: Интенсивное кардио 45 минут + разминка/заминка",
            "Неделя 3, День 1 — Питание: Цельнозерновые, Греческий салат, Протеиновый коктейль после тренировки",
            "Неделя 3, День 2 — Тренировка: Всё тело — Full Body Workout 3 круга × 10 упражнений",
            "Неделя 3, День 2 — Питание: Дневник питания — журнал еды для контроля",
            "Неделя 3, День 3 — Тренировка: Бег 5 км + растяжка после бега",
            "Неделя 3, День 3 — Питание: Ужин — пароваренные овощи + абрикосы/яблоко",
            "Неделя 3, День 4 — Тренировка: Интенсивный core — Abs circuit 4 круга",
            "Неделя 4, День 1 — Тренировка: Продвинутый HIIT 30 минут — формат табата",
            "Неделя 4, День 1 — Питание: Снижение калорий до 1500 ккал (финал месяца)",
            "Неделя 4, День 2 — Тренировка: Все мышцы с весами (если возможно)",
            "Неделя 4, День 2 — Питание: Утреннее пробуждение — вода с лимоном, затем завтрак",
            "Неделя 4, День 3 — Тренировка: Бег 6 км — скорость выше, чем на неделе 1",
            "Неделя 4, День 3 — Питание: Витамины и минералы — магний, цинк, витамин D",
            "Неделя 4, День 4 — Тренировка: Йога и медитация — полное восстановление",
            "Финальный день — Тренировка: Физический тест — вес, рост, показатели",
            "Финальный день — Результат: Анализ прогресса + план на следующий месяц",
        ],
        "english" =>
        [
            "Английский: Алфавит и основное произношение (Phonics A–Z)",
            "Английский: Числа, цвета, дни недели (Numbers, Colors, Days)",
            "Английский: Приветствия и знакомство (Greetings & Introductions)",
            "Английский: Часто используемые глаголы — be, have, do, go",
            "Английский: Настоящее время — Present Simple (I work)",
            "Английский: Present Continuous — (I am working)",
            "Английский: Прошедшее время — Past Simple (I worked)",
            "Английский: Будущее время — Future (will / going to)",
            "Английский: Артикли — a, an, the",
            "Английский: Существительные и множественное число (Plural nouns)",
            "Английский: Прилагательные и сравнение (Adjectives: big, bigger, biggest)",
            "Английский: Наречия — always, never, sometimes",
            "Английский: Предлоги места (Prepositions: in, on, at, under)",
            "Английский: Светская беседа — Small Talk",
            "Английский: Чтение и понимание текста (A1)",
            "Английский: Модальные глаголы — can, must, should, may",
            "Английский: Условные предложения (Conditionals 0 & 1)",
            "Английский: Вопросительные слова (who, what, where)",
            "Английский: Тема семьи и профессии (Family & Profession vocabulary)",
            "Английский: Тема еды и ресторана (Food & Restaurant)",
            "Английский: Тема путешествий и транспорта (Travel & Transport)",
            "Английский: Тема здоровья и тела (Health & Body)",
            "Английский: Фразовые глаголы — Phrasal verbs (get up, look for, turn on)",
            "Английский: Написание — Email и письма (Writing emails)",
            "Английский: Аудирование и понимание (Listening: диктанты A2)",
            "Английский: Present Perfect — (I have done)",
            "Английский: Пассивный залог — (It was done)",
            "Английский: Свободное говорение — Free speaking (B1 topics)",
            "Английский: Полное повторение грамматики (Grammar review)",
            "Английский: Итоговый тест + план для B1",
        ],
        "flutter" =>
        [
            "Flutter: Установка и настройка Android Studio + Flutter SDK",
            "Dart: Переменные и типы (int, String, bool, double)",
            "Dart: Условия и циклы (if/else, for, while)",
            "Dart: Функции, параметры и return",
            "Dart: Классы и объекты (OOP)",
            "Flutter: Hello World — первое приложение",
            "Flutter: Основные виджеты — Text, Container, Column, Row",
            "Flutter: Разница StatelessWidget и StatefulWidget",
            "Flutter: setState() и изменение UI",
            "Flutter: Кнопки — ElevatedButton, TextButton, IconButton",
            "Flutter: TextField и Form — получение ввода от пользователя",
            "Flutter: ListView и ListTile — списки",
            "Flutter: Navigator — переход между экранами",
            "Flutter: AppBar и BottomNavigationBar",
            "Flutter: Изображения и иконки",
            "Flutter: Stack и Positioned",
            "Flutter: Padding, Margin, SizedBox, Expanded",
            "Flutter: Colors, ThemeData, Dark/Light mode",
            "Flutter: HTTP и http package — работа с API",
            "Flutter: JSON parsing — jsonDecode()",
            "Flutter: FutureBuilder и async/await",
            "Flutter: Provider — управление состоянием",
            "Flutter: SharedPreferences — локальные данные",
            "Flutter: StreamBuilder и Stream",
            "Flutter: Firebase Auth — вход/регистрация",
            "Flutter: Cloud Firestore — облачная база данных",
            "Flutter: Push Notifications — Firebase Messaging",
            "Flutter: Анимации — AnimationController, Tween",
            "Flutter: Build & Deploy — APK для Android",
            "Flutter: Итоговый проект — полное приложение с нуля",
        ],
        "business" =>
        [
            "Бизнес: Бизнес-идея — поиск и оценка проблемы",
            "Бизнес: Анализ рынка — кто мой клиент? (ICP)",
            "Бизнес: Анализ конкурентов — SWOT analysis",
            "Бизнес: Бизнес-модель — Business Model Canvas",
            "Бизнес: Как написать бизнес-план (Business Plan)",
            "Бизнес: Расходы и доходы — стартовый бюджет",
            "Бизнес: Маркетинг — кому, где и когда продаю?",
            "Бизнес: SMM — Instagram, Telegram для бизнеса",
            "Бизнес: Бренд и логотип — название и образ компании",
            "Бизнес: Первая продажа — как найти первого клиента?",
            "Бизнес: Психология цены — pricing strategy",
            "Бизнес: Общение с клиентом — скрипты продаж",
            "Бизнес: Обратная связь — как получаем отзывы?",
            "Бизнес: Организация работы — Trello, Notion, Asana",
            "Бизнес: MVP — что такое минимально жизнеспособный продукт?",
            "Бизнес: Поиск инвестора — привлечение финансирования",
            "Бизнес: Pitch deck — презентация идеи инвестору",
            "Бизнес: Бизнес-право — регистрация компании, налоги",
            "Бизнес: Управление персоналом — как нанимать сотрудников?",
            "Бизнес: KPI и OKR — измерение результатов",
            "Бизнес: E-commerce — онлайн-продажи",
            "Бизнес: Логистика и снабжение",
            "Бизнес: Послепродажное обслуживание (after-sales)",
            "Бизнес: Типичные проблемы стартапа и их решения",
            "Бизнес: Быстрый рост — growth hacking",
            "Бизнес: Партнёрство и нетворкинг (networking)",
            "Бизнес: Финансовый анализ — P&L, Cash flow",
            "Бизнес: Корпоративная социальная ответственность — CSR",
            "Бизнес: Анализ прогресса — что нужно изменить?",
            "Бизнес: План развития на 6 месяцев и 1 год",
        ],
        "exam" =>
        [
            "Экзамен: Организация рабочего места и план подготовки",
            "Экзамен: Тема 1 — чтение и конспект",
            "Экзамен: Тема 2 — основные понятия",
            "Экзамен: Тема 3 — формулы и правила",
            "Экзамен: Тема 4 — анализ и примеры",
            "Экзамен: Повторение Темы 1–4 + тестовые вопросы",
            "Экзамен: Тема 5 — чтение",
            "Экзамен: Тема 6 — конспект и заметки",
            "Экзамен: Тема 7 — примеры и упражнения",
            "Экзамен: Тема 8 — практические задания",
            "Экзамен: Повторение Темы 5–8 + проверка",
            "Экзамен: Тема 9 — глубокий анализ",
            "Экзамен: Тема 10 — чтение",
            "Экзамен: Повторение Темы 9–10 + полный тест",
            "Экзамен: Исправление ошибок и слабых мест",
            "Экзамен: Темы 11–12 — чтение",
            "Экзамен: Практика общего теста — 1 час",
            "Экзамен: Повторение всех формул и определений",
            "Экзамен: Имитационный экзамен на время",
            "Экзамен: Разбор результата и исправление",
            "Экзамен: Трудные темы — глубокое повторение",
            "Экзамен: Повторение всех конспектов",
            "Экзамен: Общая проверка 2 — полный тест",
            "Экзамен: День отдыха — обзор и спокойствие",
            "Экзамен: Изучение неизвестных моментов",
            "Экзамен: Последнее повторение всех тем",
            "Экзамен: Итоговый пробный экзамен",
            "Экзамен: Полный отдых — сохрани свежесть ума",
            "Экзамен: Подготовка последнего дня — краткий обзор",
            "Экзамен: День экзамена — верь в себя, удача!",
        ],
        "react" =>
        [
            "React: Установка — Node.js, npm, create-react-app / Vite",
            "React: JSX — HTML в JavaScript",
            "React: Функциональные компоненты",
            "React: Props — передача данных между компонентами",
            "React: useState — локальное состояние",
            "React: useEffect — побочные эффекты",
            "React: Списки и key prop",
            "React: Обработка событий (onClick, onChange)",
            "React: Условный рендеринг (conditional rendering)",
            "React: Формы и controlled components",
            "React: useContext — общие данные",
            "React: useReducer — сложное состояние",
            "React: useMemo и useCallback — оптимизация",
            "React: useRef — ссылка на DOM элемент",
            "React: React Router — навигация",
            "React: Fetch и async/await в React",
            "React: TanStack Query — server state",
            "React: Zustand — управление состоянием",
            "React: Tailwind CSS с React",
            "React: TypeScript с React",
            "React: Тесты — React Testing Library",
            "React: Next.js — основы",
            "React: Next.js — App Router",
            "React: Next.js — Server Components",
            "React: Next.js — API Routes",
            "React: Deployment — Vercel",
            "React: Error Boundaries",
            "React: Performance — Profiler",
            "React: Accessibility (a11y)",
            "React: Итоговый проект — Full-stack Next.js App",
        ],
        _ => Enumerable.Range(1, 90).Select(d => $"{label}: Урок {d}").ToList()
    };

    /// <summary>
    /// Converts a local wall-clock time (hour:minute on the given date) to UTC,
    /// using the IANA/Windows timezone ID provided by the caller.
    /// Falls back to treating the time as UTC if the timezone is unknown.
    /// </summary>
    private static DateTime ToUtcFromLocal(DateTime date, int hour, int minute, string? userTimeZone)
    {
        var local = date.Date.AddHours(hour).AddMinutes(minute);
        try
        {
            if (string.IsNullOrWhiteSpace(userTimeZone))
                return DateTime.SpecifyKind(local, DateTimeKind.Utc);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(userTimeZone);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), tz);
        }
        catch
        {
            return DateTime.SpecifyKind(local, DateTimeKind.Utc);
        }
    }

    private static List<GeminiSuggestedTask> BuildDefaultSuggestions(DateTime date, string? userTimeZone = null)
    {
        return
        [
            new GeminiSuggestedTask(
                "Медитация и глубокое дыхание",
                "10 минут медитации для успокоения ума и снижения стресса.",
                ToUtcFromLocal(date, 7, 30, userTimeZone),
                10,
                "Исследования показывают, что ежедневная медитация повышает продуктивность на 23%."),

            new GeminiSuggestedTask(
                "Чтение книги или профессиональной статьи",
                "30 минут чтения для профессионального развития и получения знаний.",
                ToUtcFromLocal(date, 21, 0, userTimeZone),
                30,
                "Успешные руководители читают минимум 30 минут в день."),

            new GeminiSuggestedTask(
                "Питьё воды и короткая разминка",
                "Каждые 2 часа делай 5-минутную разминку и выпивай стакан воды.",
                ToUtcFromLocal(date, 11, 0, userTimeZone),
                5,
                "Употребление 8 стаканов воды в день улучшает энергию и концентрацию."),

            new GeminiSuggestedTask(
                "Планирование следующего дня",
                "10 минут на запись важных дел на завтра и расстановку приоритетов.",
                ToUtcFromLocal(date, 21, 30, userTimeZone),
                10,
                "Вечернее планирование сокращает утреннее время на 1 час."),

            new GeminiSuggestedTask(
                "Качественное время с семьёй",
                "30 минут без телефона — поговори или поиграй с семьёй.",
                ToUtcFromLocal(date, 19, 30, userTimeZone),
                30,
                "Крепкие семейные отношения — основа счастья и ежедневной мотивации.")
        ];
    }

    private static List<GeminiScheduledTask> ParseTasksFromText(string text, DateTime date, string? userTimeZone = null)
    {
        var tasks = new List<GeminiScheduledTask>();

        // Matches: "в 8:00", "в 10:00-10:30", "в 10:30-12:00", "Утром в 8"
        var pattern = @"[Вв]\s+(\d{1,2}(?::\d{2})?)(?:\s*[-–]\s*(\d{1,2}(?::\d{2})?))?(.+?)(?=\.\s*[А-Яа-я]|[Вв]\s+\d|\z)";
        var matches = Regex.Matches(text, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var startStr   = match.Groups[1].Value.Trim();
            var endStr     = match.Groups[2].Value.Trim();
            var rawDesc    = match.Groups[3].Value.Trim().TrimEnd('.');

            if (!TryParseTime(startStr, out var startH, out var startM)) continue;
            if (string.IsNullOrWhiteSpace(rawDesc)) continue;

            // Convert local time to UTC using the caller's timezone
            var scheduledAt = ToUtcFromLocal(date, startH, startM, userTimeZone);

            var duration = 60;
            if (!string.IsNullOrEmpty(endStr) && TryParseTime(endStr, out var endH, out var endM))
            {
                var diff = endH * 60 + endM - (startH * 60 + startM);
                if (diff > 0) duration = diff;
            }

            var title = rawDesc.Length > 60 ? rawDesc[..60].Trim() : rawDesc;

            tasks.Add(new GeminiScheduledTask(
                title,
                rawDesc,
                scheduledAt,
                duration,
                null,
                RecurrenceType.None));
        }

        // If regex found nothing, create one generic task
        if (tasks.Count == 0)
            tasks.Add(new GeminiScheduledTask(
                "Ежедневные дела",
                text.Length > 200 ? text[..200].Trim() + "..." : text,
                ToUtcFromLocal(date, 9, 0, userTimeZone),
                60,
                null,
                RecurrenceType.None));

        return tasks;
    }

    private static bool TryParseTime(string s, out int hour, out int minute)
    {
        hour = 0; minute = 0;
        var parts = s.Split(':');
        if (!int.TryParse(parts[0], out hour)) return false;
        if (parts.Length > 1 && !int.TryParse(parts[1], out minute)) return false;
        return hour is >= 0 and <= 23 && minute is >= 0 and <= 59;
    }

    private static GeminiScheduleResult ParseScheduleResult(string json)
    {
        try
        {
            var doc = JsonNode.Parse(json);
            var scheduledTasks = new List<GeminiScheduledTask>();
            var suggestedTasks = new List<GeminiSuggestedTask>();

            var scheduled = doc?["scheduledTasks"]?.AsArray();
            if (scheduled is not null)
            {
                foreach (var item in scheduled)
                {
                    var title = item?["title"]?.GetValue<string>() ?? string.Empty;
                    var description = item?["description"]?.GetValue<string>();
                    var scheduledAtStr = item?["scheduledAtUtc"]?.GetValue<string>() ?? string.Empty;
                    var estimatedMinutes = item?["estimatedMinutes"]?.GetValue<int>() ?? 30;
                    var rationale = item?["rationale"]?.GetValue<string>();
                    var recurrenceStr = item?["recurrence"]?.GetValue<string>() ?? "None";

                    if (DateTime.TryParse(scheduledAtStr, out var scheduledAt))
                    {
                        Enum.TryParse<RecurrenceType>(recurrenceStr, true, out var recurrence);
                        scheduledTasks.Add(new GeminiScheduledTask(title, description, scheduledAt.ToUniversalTime(), estimatedMinutes, rationale, recurrence));
                    }
                }
            }

            var suggested = doc?["suggestedAdditionalTasks"]?.AsArray();
            if (suggested is not null)
            {
                foreach (var item in suggested)
                {
                    var title = item?["title"]?.GetValue<string>() ?? string.Empty;
                    var description = item?["description"]?.GetValue<string>();
                    var scheduledAtStr = item?["scheduledAtUtc"]?.GetValue<string>() ?? string.Empty;
                    var estimatedMinutes = item?["estimatedMinutes"]?.GetValue<int>() ?? 30;
                    var rationale = item?["rationale"]?.GetValue<string>() ?? string.Empty;

                    if (DateTime.TryParse(scheduledAtStr, out var scheduledAt))
                        suggestedTasks.Add(new GeminiSuggestedTask(title, description, scheduledAt.ToUniversalTime(), estimatedMinutes, rationale));
                }
            }

            return new GeminiScheduleResult(scheduledTasks, suggestedTasks);
        }
        catch
        {
            return new GeminiScheduleResult([], []);
        }
    }
}

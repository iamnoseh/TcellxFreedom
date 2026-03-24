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
                Rationale: "Вазифа тибқи авлавият ва муҳлати нақша ҷадвалбандӣ шуд.",
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

    private static readonly int[] _retryDelaysSeconds = [15, 30, 60];

    private async Task<string> CallGeminiAsync(object requestBody, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Gemini");
        var url = $"/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";
        var json = JsonSerializer.Serialize(requestBody); // immutable string — safe to reuse

        const int maxAttempts = 4;
        Exception? lastEx = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Create a NEW HttpRequestMessage + StringContent on every attempt.
            // HttpContent streams are exhausted after first send — this is the root cause
            // of the previous 403s that occurred when Polly retried with the same content.
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await client.SendAsync(req, ct);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    throw new InvalidOperationException("Gemini 403: quota exhausted or invalid API key.");

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt >= maxAttempts - 1)
                        throw new InvalidOperationException("Gemini rate limit exceeded after all retry attempts.");

                    var delay = _retryDelaysSeconds[Math.Min(attempt, _retryDelaysSeconds.Length - 1)];
                    if (response.Headers.TryGetValues("Retry-After", out var vals)
                        && int.TryParse(vals.FirstOrDefault(), out var serverDelay) && serverDelay > 0)
                        delay = Math.Max(delay, serverDelay);

                    _logger.LogWarning("Gemini 429 on attempt {Attempt}. Waiting {Delay}s before retry.", attempt, delay);
                    await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                    continue;
                }

                if ((int)response.StatusCode >= 500)
                {
                    if (attempt >= maxAttempts - 1)
                        throw new InvalidOperationException($"Gemini {(int)response.StatusCode} after all retry attempts.");
                    await Task.Delay(TimeSpan.FromSeconds(_retryDelaysSeconds[Math.Min(attempt, 2)]), ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonNode.Parse(body);
                var text = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
                return text ?? throw new InvalidOperationException("Empty response from Gemini.");
            }
            catch (InvalidOperationException) { throw; }
            catch (TaskCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { lastEx = ex; }

            if (attempt < maxAttempts - 1)
                await Task.Delay(TimeSpan.FromSeconds(_retryDelaysSeconds[Math.Min(attempt, 2)]), ct);
        }

        throw new InvalidOperationException("AI service temporarily unavailable.", lastEx);
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
            instruction = "Вазифаҳои зеринро дар муддати муайяншуда чобачо кун. Барои ҳар вазифа вақти мушаххас таъин кун (дар асоси навъи вазифа ва авлавият). Муддати тахминии иҷроро бо дақиқа нишон деҳ. 2-4 вазифаи иловагии муфид пешниход кун. Унвон, тавсиф ва сабаби пешниход бояд ба забони тоҷикӣ бошанд. Ҳамаи вақтҳо дар формати UTC ISO8601 бошанд.",
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
                    new { title = "сатр (ба тоҷикӣ)", description = "сатр ё null (ба тоҷикӣ)", scheduledAtUtc = "ISO8601 datetime", estimatedMinutes = "адади бутун", rationale = "сатр ё null (ба тоҷикӣ)", recurrence = "None ё Daily ё Weekly" }
                },
                suggestedAdditionalTasks = new[]
                {
                    new { title = "сатр (ба тоҷикӣ)", description = "сатр ё null (ба тоҷикӣ)", scheduledAtUtc = "ISO8601 datetime", estimatedMinutes = "адади бутун", rationale = "сатр - ҳатмист, сабаби пешниходро ба тоҷикӣ нависед" }
                }
            }
        });

        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = "Ту мутахассиси банақшагирии шахсӣ ва менеҷменти вақт ҳастӣ. Вазифаи ту ин аст, ки вазифаҳои корбарро дар ҷадвали рӯзона ба таври оптималӣ ҷойгир кунӣ ва вазифаҳои иловагии муфид пешниход кунӣ. Ҳамаи матнҳо (title, description, rationale) бояд ба забони ТОҶИКӢ бошанд. Фақат JSON-и дуруст баргардон, ҳеҷ шарҳи берунӣ надеҳ." } }
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
            instruction = "Омори иҷрои вазифаҳои корбарро таҳлил кун ва 3-5 тавсияи мушаххас ва амалӣ барои беҳтар кардани натиҷаҳо пешниход кун. Ҷавобро ба шакли массиви JSON аз сатрҳо деҳ. Ҳамаи тавсияҳо бояд ба забони ТОҶИКӢ бошанд.",
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
            sb.AppendLine("5. All title, description, planTitle, planDescription fields MUST be in TAJIK language.");
            sb.AppendLine("6. suggestedAdditionalTasks MUST be an empty array [].");
            sb.AppendLine();
            sb.AppendLine("Day schedule (use these exact dates):");
            for (int d = 0; d < totalDays; d++)
            {
                var dayDate = request.Date.AddDays(d);
                sb.AppendLine($"  Day {d + 1}: {dayDate:yyyy-MM-dd}");
            }
            sb.AppendLine();
            sb.AppendLine("Example of what GOOD output looks like for a Python plan (titles must be in Tajik):");
            sb.AppendLine("  Day 1: 'Python: Насб ва муҳити кор (VSCode, Python 3)' — basic setup");
            sb.AppendLine("  Day 2: 'Python: Тағйирёбандаҳо ва навъҳои маълумот' — variables & data types");
            sb.AppendLine("  Day 3: 'Python: Операторҳои шартӣ (if/elif/else)' — conditionals");
            sb.AppendLine("  Day 4: 'Python: Давраҳо (for, while)' — loops");
            sb.AppendLine("  Day 5: 'Python: Рӯйхатҳо ва лӯлаҳо (list, tuple)' — collections");
            sb.AppendLine("  ... and so on, progressively more advanced each day.");
            instruction = sb.ToString();
        }
        else
        {
            instruction = $"You are a personal productivity coach. Extract tasks and times from the user's message. " +
                $"TIME ZONE: User is in UTC{utcOffsetStr}. Convert all times to UTC (ISO8601 with Z). " +
                $"All title/description/planTitle/planDescription must be in TAJIK language. " +
                $"Set suggestedAdditionalTasks to empty array [].";
        }

        var userMessage = JsonSerializer.Serialize(new
        {
            instruction,
            date = request.Date.ToString("yyyy-MM-dd"),
            endDate = request.EndDate?.ToString("yyyy-MM-dd"),
            totalDays,
            userTimeZone = request.UserTimeZone,
            utcOffset = utcOffsetStr,
            userText = request.FreeText,
            responseSchema = new
            {
                planTitle = "string (Tajik)",
                planDescription = "string (Tajik)",
                scheduledTasks = new[]
                {
                    new { title = "string (Tajik, unique per day)", description = "string (Tajik) or null", scheduledAtUtc = "2026-03-24T17:00:00Z", estimatedMinutes = 90, rationale = "string (Tajik) or null", recurrence = "None" }
                },
                suggestedAdditionalTasks = Array.Empty<object>()
            }
        });

        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = "You are an expert curriculum designer and personal coach. When building multi-day plans, you MUST produce one unique, progressively harder task per day — like a real teacher designing a course. Task titles and descriptions must be in TAJIK language. Return ONLY valid JSON, no extra text." } }
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
            var planTitle = doc?["planTitle"]?.GetValue<string>() ?? "Нақшаи рӯзона";
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
            return new GeminiChatScheduleResult("Нақшаи рӯзона", null, [], []);
        }
    }

    private static GeminiChatScheduleResult BuildChatFallbackResult(GeminiChatScheduleRequest request)
    {
        if (request.EndDate.HasValue && request.EndDate.Value.Date > request.Date.Date)
            return BuildMultiDayCurriculumFallback(request);

        var tasks = ParseTasksFromText(request.FreeText, request.Date, request.UserTimeZone);
        return new GeminiChatScheduleResult("Нақшаи рӯзона", "Нақша тибқи матни корбар тартиб дода шуд.", tasks, []);
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
                : $"{topicLabel}: Дарси {day + 1}";

            tasks.Add(new GeminiScheduledTask(
                title,
                $"Рӯзи {day + 1} аз {totalDays}: {title}",
                scheduledAt,
                durationMins,
                null,
                RecurrenceType.None));
        }

        return new GeminiChatScheduleResult(
            $"Курси {topicLabel} ({totalDays} рӯз)",
            $"{totalDays}-рӯза нақшаи омӯзиши {topicLabel}",
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
        if (t.Contains("arabic") || t.Contains("арабӣ"))              return ("arabic",      "Забони арабӣ");
        if (t.Contains("english") || t.Contains("англис") ||
            t.Contains("инглис") || t.Contains("английск"))           return ("english",     "Забони англисӣ");
        if (t.Contains("бизнес") || t.Contains("business") ||
            t.Contains("стартап") || t.Contains("startup") ||
            t.Contains("тиҷорат") || t.Contains("тичорат"))           return ("business",    "Бизнес");
        if (t.Contains("имтиҳон") || t.Contains("имтихон") ||
            t.Contains("exam") || t.Contains("омода") ||
            t.Contains("тест") || t.Contains("test"))                 return ("exam",        "Омодагӣ ба имтиҳон");
        // Fitness: weight loss, exercise, nutrition keywords
        if (t.Contains("варзиш") || t.Contains("sport") || t.Contains("фитнес") ||
            t.Contains("машк") || t.Contains("вазн") || t.Contains("хурок") ||
            t.Contains("диет") || t.Contains("калори") || t.Contains("мушак") ||
            t.Contains("кг") || t.Contains("фарбех") || t.Contains("давид"))
                                                                       return ("fitness",     "Варзиш ва тағзия");
        if (t.Contains("математик") || t.Contains("math"))            return ("math",        "Математика");
        // Generic fallback
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('.', ',', '!', '?', ':'))
            .Where(w => w.Length > 4)
            .ToList();
        var label = words.Count > 0 ? words[0] : "Омӯзиш";
        return ("generic", label);
    }

    private static List<string> GetCurriculum(string key, string label) => key switch
    {
        "python" =>
        [
            "Python: Насб ва танзими муҳити кор (Python + VSCode)",
            "Python: Тағйирёбандаҳо ва навъҳои маълумот (int, str, float, bool)",
            "Python: Операторҳои риёзӣ ва муқоисавӣ",
            "Python: Сатрҳо (str) ва амалиётҳои онҳо",
            "Python: Шарти if / elif / else",
            "Python: Давраи for ва range()",
            "Python: Давраи while, break ва continue",
            "Python: Функсияҳо — def ва return",
            "Python: Аргументҳои функсия (*args, **kwargs)",
            "Python: Рӯйхатҳо (list) ва амалиётҳои онҳо",
            "Python: Лӯлаҳо (tuple) ва маҷмӯаҳо (set)",
            "Python: Луғатҳо (dict) — калид ва қимат",
            "Python: List comprehension ва dict comprehension",
            "Python: Коркарди файлҳо (open, read, write)",
            "Python: Истисноҳо — try / except / finally",
            "Python: Модулҳо ва import (os, sys, math)",
            "Python: Барномасозии объектӣ — class ва object",
            "Python: Ворисхӯрӣ (inheritance) ва __init__",
            "Python: Декораторҳо ва @property",
            "Python: Генераторҳо (generators) ва yield",
            "Python: Регулярные выражения (re)",
            "Python: Кор бо санаю вақт (datetime, timedelta)",
            "Python: JSON ва CSV — хондан ва навиштан",
            "Python: Санҷишҳо — pytest ва unittest",
            "Python: NumPy — асосҳои ҳисоббарории рақамӣ",
            "Python: Pandas — таҳлили маълумотҳо",
            "Python: Matplotlib — нақшаҳои визуалӣ",
            "Python: Requests — кор бо API-ҳои берунӣ",
            "Python: Flask — сохтани API-и оддии веб",
            "Python: Лоиҳаи ниҳоӣ — татбиқи ҳамаи донишҳо",
        ],
        "javascript" =>
        [
            "JS: Асосҳо — let, const, var ва навъҳо",
            "JS: Операторҳо ва сатрҳо (template literals)",
            "JS: Шартҳо (if/else) ва switch",
            "JS: Давраҳо — for, while, forEach",
            "JS: Функсияҳо ва arrow functions",
            "JS: Масивҳо (Array) ва амалиётҳо",
            "JS: Объектҳо (Object) ва деструктуратсия",
            "JS: Map, Filter, Reduce",
            "JS: DOM — дастрасӣ ва тағйир додан",
            "JS: Рӯйдодҳо (Events) ва addEventListener",
            "JS: Promise ва async/await",
            "JS: Fetch API — кор бо серверҳо",
            "JS: ES6+ — spread, rest, optional chaining",
            "JS: Модулҳо — import/export",
            "JS: Синфҳо (Class) ва OOP",
            "JS: localStorage ва sessionStorage",
            "JS: Error handling — try/catch",
            "JS: Regular Expressions",
            "JS: Closure ва scope",
            "JS: Prototype ва prototype chain",
            "JS: TypeScript — асосҳо",
            "JS: NPM ва package.json",
            "JS: Node.js — сервери оддӣ",
            "JS: Express.js — API сохтан",
            "JS: React — асосҳои JSX ва компонентҳо",
            "JS: React — useState ва useEffect",
            "JS: React — props ва children",
            "JS: React — React Router",
            "JS: Тест навиштан — Jest",
            "JS: Лоиҳаи ниҳоӣ — Full-stack mini app",
        ],
        "fitness" =>
        [
            "Ҳафта 1, Рӯзи 1 — Машқ: Гармкунӣ + 30 дақиқа давидани оҳиста",
            "Ҳафта 1, Рӯзи 1 — Хурок: Субҳона овсянка + об, нисфирӯзӣ мурғ + сабзавот, шом моҳӣ + салат",
            "Ҳафта 1, Рӯзи 2 — Машқ: Push-up 3×15, Plank 3×30сон, Squat 3×20",
            "Ҳафта 1, Рӯзи 2 — Хурок: Калориҳо 1600-1800 ккал, протеин зиёд, қанд кам",
            "Ҳафта 1, Рӯзи 3 — Машқ: Кардио 40 дақиқа (давидан ё велосипед)",
            "Ҳафта 1, Рӯзи 3 — Хурок: Субҳона тухм + нон тира, нисфирӯзӣ гушт + биринҷ қаҳваранг",
            "Ҳафта 1, Рӯзи 4 — Машқ: Истироҳат + Yoga барои чандирӣ (15 дақиқа)",
            "Ҳафта 2, Рӯзи 1 — Машқ: HIIT 20 дақиқа — Burpee, Mountain climber, Jump squat",
            "Ҳафта 2, Рӯзи 1 — Хурок: Оби зиёд (2-2.5л/рӯз), мева ба ҷои ширинӣ",
            "Ҳафта 2, Рӯзи 2 — Машқ: Мушакҳои сина ва китф — Dumbbell press, Lateral raise",
            "Ҳафта 2, Рӯзи 2 — Хурок: Субҳона чой сабз + тухм, шом каш нашавӣ баъд аз 20:00",
            "Ҳафта 2, Рӯзи 3 — Машқ: Пиллаҳои пой — Lunges 3×15, Deadlift бо вазни бадан",
            "Ҳафта 2, Рӯзи 3 — Хурок: Протеин 1.5г/кг вазн — мурғ, лӯбиё, тухм, моҳӣ",
            "Ҳафта 2, Рӯзи 4 — Машқ: Пушт ва core — Plank variations, Superman, Bird-dog",
            "Ҳафта 3, Рӯзи 1 — Машқ: Кардио шадид 45 дақиқа + гармкунӣ/совутиш",
            "Ҳафта 3, Рӯзи 1 — Хурок: Гандум, Салат Греция, Коктейли протеин баъд аз машқ",
            "Ҳафта 3, Рӯзи 2 — Машқ: Тамоми бадан — Full Body Workout 3 давра × 10 машқ",
            "Ҳафта 3, Рӯзи 2 — Хурок: Ёддошти хурок — журнали хурок барои назорат",
            "Ҳафта 3, Рӯзи 3 — Машқ: Давидан 5 км + stretch баъд аз давидан",
            "Ҳафта 3, Рӯзи 3 — Хурок: Хуроки шаб — сабзавоти буғкарда + зардолу/себ",
            "Ҳафта 3, Рӯзи 4 — Машқ: Core intensive — Abs circuit 4 давра",
            "Ҳафта 4, Рӯзи 1 — Машқ: HIIT пешрафта 30 дақиқа — табата формат",
            "Ҳафта 4, Рӯзи 1 — Хурок: Калория кам кардан то 1500 ккал (ниҳоии моҳ)",
            "Ҳафта 4, Рӯзи 2 — Машқ: Мушакҳои тамоми бадан бо вазн (агар имкон бошад)",
            "Ҳафта 4, Рӯзи 2 — Хурок: Обгармшавӣ субҳ — об бо лимон, сипас субҳона",
            "Ҳафта 4, Рӯзи 3 — Машқ: Давидан 6 км — суръат назар ба ҳафтаи 1 зиёдтар",
            "Ҳафта 4, Рӯзи 3 — Хурок: Витаминҳо ва маъданҳо — магний, рӯҳ, витамини D",
            "Ҳафта 4, Рӯзи 4 — Машқ: Йога ва медитатсия — барқарорсозии комил",
            "Рӯзи ниҳоӣ — Машқ: Санҷиши физикӣ — вазн, кадд, нишондиҳандаҳо",
            "Рӯзи ниҳоӣ — Натиҷа: Таҳлили пешрафт + нақшаи моҳи оянда",
        ],
        "english" =>
        [
            "Англисӣ: Алифбо ва талаффузи асосӣ (Phonics A–Z)",
            "Англисӣ: Ададҳо, рангҳо, рӯзҳои ҳафта (Numbers, Colors, Days)",
            "Англисӣ: Саломутия ва шиносоӣ (Greetings & Introductions)",
            "Англисӣ: Феълҳои пурмасраф — be, have, do, go",
            "Англисӣ: Замони ҳозира — Present Simple (I work)",
            "Англисӣ: Present Continuous — (I am working)",
            "Англисӣ: Замони гузашта — Past Simple (I worked)",
            "Англисӣ: Замони оянда — Future (will / going to)",
            "Англисӣ: Артиклҳо — a, an, the",
            "Англисӣ: Исмҳои зиёдшаванда ва ҷамъ (Plural nouns)",
            "Англисӣ: Сифатҳо ва муқоиса (Adjectives: big, bigger, biggest)",
            "Англисӣ: Зарфҳо — always, never, sometimes",
            "Англисӣ: Пешоянди ҷой (Prepositions: in, on, at, under)",
            "Англисӣ: Сухани кӯтоҳи рӯзмарра — Small Talk",
            "Англисӣ: Хонда гирифтан — Reading comprehension (A1)",
            "Англисӣ: Modal verbs — can, must, should, may",
            "Англисӣ: Шартҳо — If sentences (Conditionals 0 & 1)",
            "Англисӣ: Саволҳои умумӣ ва посух (Question words: who, what, where)",
            "Англисӣ: Мавзӯи оила ва касб (Family & Profession vocabulary)",
            "Англисӣ: Мавзӯи хӯрок ва хӯрокхурӣ (Food & Restaurant)",
            "Англисӣ: Мавзӯи сафар ва транспорт (Travel & Transport)",
            "Англисӣ: Мавзӯи саломатӣ ва бадан (Health & Body)",
            "Англисӣ: Иборасозӣ — Phrasal verbs (get up, look for, turn on)",
            "Англисӣ: Навиштан — Email ва хат (Writing emails)",
            "Англисӣ: Шунидан ва фаҳмидан (Listening: диктантҳо A2)",
            "Англисӣ: Present Perfect — (I have done)",
            "Англисӣ: Passive voice — (It was done)",
            "Англисӣ: Мубоҳисаи озод — Free speaking (B1 topics)",
            "Англисӣ: Такрори пурраи грамматика (Grammar review)",
            "Англисӣ: Санҷиши ниҳоӣ + нақша барои B1",
        ],
        "flutter" =>
        [
            "Flutter: Насб ва танзими Android Studio + Flutter SDK",
            "Dart: Тағйирёбандаҳо, навъҳо (int, String, bool, double)",
            "Dart: Шартҳо ва давраҳо (if/else, for, while)",
            "Dart: Функсияҳо, параметрҳо ва return",
            "Dart: Классҳо ва объектҳо (OOP)",
            "Flutter: Hello World — аввалин app",
            "Flutter: Widget-ҳои асосӣ — Text, Container, Column, Row",
            "Flutter: StatelessWidget ва StatefulWidget фарқ",
            "Flutter: setState() ва тағйири UI",
            "Flutter: Button-ҳо — ElevatedButton, TextButton, IconButton",
            "Flutter: TextField ва Form — гирифтани ворид аз корбар",
            "Flutter: ListView ва ListTile — рӯйхатҳо",
            "Flutter: Navigator — гузаштан байни саҳифаҳо",
            "Flutter: AppBar ва BottomNavigationBar",
            "Flutter: Image ва Icon-ҳо",
            "Flutter: Stack ва Positioned",
            "Flutter: Padding, Margin, SizedBox, Expanded",
            "Flutter: Colors, ThemeData, Dark/Light mode",
            "Flutter: HTTP ва http package — кор бо API",
            "Flutter: JSON parsing — jsonDecode()",
            "Flutter: FutureBuilder ва async/await",
            "Flutter: Provider — state management",
            "Flutter: SharedPreferences — маълумоти маҳаллӣ",
            "Flutter: StreamBuilder ва Stream",
            "Flutter: Firebase Auth — login/signup",
            "Flutter: Cloud Firestore — маълумотгоҳи абрӣ",
            "Flutter: Push Notifications — Firebase Messaging",
            "Flutter: Animations — AnimationController, Tween",
            "Flutter: Build & Deploy — APK барои Android",
            "Flutter: Лоиҳаи ниҳоӣ — App-и пурра аз нол",
        ],
        "business" =>
        [
            "Бизнес: Идеяи бизнес — кашф ва арзёбии мушкил",
            "Бизнес: Таҳлили бозор — кист мизоҷи ман? (ICP)",
            "Бизнес: Таҳлили рақибон — SWOT analysis",
            "Бизнес: Модели бизнес — Business Model Canvas",
            "Бизнес: Нақшаи бизнес (Business Plan) чӣ гуна навишта мешавад",
            "Бизнес: Хароҷот ва даромад — бюджети оғозӣ",
            "Бизнес: Маркетинг — кӣ, куҷо, чӣ вақт мефурӯшам?",
            "Бизнес: SMM — Instagram, Telegram барои бизнес",
            "Бизнес: Бренд ва логотип — ном ва тасвири ширкат",
            "Бизнес: Аввалин фурӯш — чӣ гуна мизоҷи нахустро ёбем?",
            "Бизнес: Психологияи нарх — pricing strategy",
            "Бизнес: Тарзи муошират бо мизоҷ — скриптҳои фурӯш",
            "Бизнес: Feedback — чӣ гуна бозгӯй мегирем?",
            "Бизнес: Ташкили кор — Trello, Notion, Asana",
            "Бизнес: MVP — маҳсулоти минималии корӣ чист?",
            "Бизнес: Ҷустуҷӯи сармоягузор — маблағгузорӣ",
            "Бизнес: Pitch deck — тақдим кардани идея ба сармоягузор",
            "Бизнес: Ҳуқуқи бизнес — қайди ширкат, андоз",
            "Бизнес: Идоракунии кормандон — чӣ гуна ба кор мегирем?",
            "Бизнес: KPI ва OKR — санҷиши натиҷаҳо",
            "Бизнес: E-commerce — фурӯши онлайн",
            "Бизнес: Логистика ва таъминот",
            "Бизнес: Хизматрасонӣ баъд аз фурӯш (after-sales)",
            "Бизнес: Масъалаҳои маъмулии стартап ва роҳҳои ҳалли онҳо",
            "Бизнес: Рушди тез — growth hacking",
            "Бизнес: Партнёрӣ ва шабакасозӣ (networking)",
            "Бизнес: Таҳлили молиявӣ — P&L, Cash flow",
            "Бизнес: Масъулияти иҷтимоӣ — CSR",
            "Бизнес: Баррасии пешрафт — чи тағйир бояд дод?",
            "Бизнес: Нақшаи рушди 6-моҳа ва 1-сола",
        ],
        "exam" =>
        [
            "Имтиҳон: Ташкили ҷой ва нақшаи омодагӣ",
            "Имтиҳон: Мавзӯи 1 — хонда гирифтан ва конспект",
            "Имтиҳон: Мавзӯи 2 — мафҳумҳои асосӣ",
            "Имтиҳон: Мавзӯи 3 — формулаҳо ва қоидаҳо",
            "Имтиҳон: Мавзӯи 4 — таҳлил ва мисолҳо",
            "Имтиҳон: Такрори Мавзӯи 1–4 + саволҳои тестӣ",
            "Имтиҳон: Мавзӯи 5 — хонда гирифтан",
            "Имтиҳон: Мавзӯи 6 — конспект ва ёддошт",
            "Имтиҳон: Мавзӯи 7 — мисолҳо ва машқҳо",
            "Имтиҳон: Мавзӯи 8 — вазифаҳои амалӣ",
            "Имтиҳон: Такрори Мавзӯи 5–8 + санҷиш",
            "Имтиҳон: Мавзӯи 9 — таҳлили чуқур",
            "Имтиҳон: Мавзӯи 10 — хонда гирифтан",
            "Имтиҳон: Такрори Мавзӯи 9–10 + тести пурра",
            "Имтиҳон: Ислоҳи хатоҳо ва нуқтаҳои суст",
            "Имтиҳон: Мавзӯи 11–12 — хонда гирифтан",
            "Имтиҳон: Машқи тести умумӣ — 1 соат",
            "Имтиҳон: Такрори тамоми формулаҳо ва таъриф",
            "Имтиҳон: Санҷиши вақт — имтиҳони имитатсионӣ",
            "Имтиҳон: Баррасии натиҷа ва ислоҳ",
            "Имтиҳон: Мавзӯҳои душвор — такрори чуқур",
            "Имтиҳон: Такрори тамоми конспектҳо",
            "Имтиҳон: Санҷиши умумӣ 2 — тести пурра",
            "Имтиҳон: Рӯзи истироҳат — мурор ва оромиш",
            "Имтиҳон: Нуқтаҳои нодонистаро ёд гирифтан",
            "Имтиҳон: Такрори охирини ҳамаи мавзӯҳо",
            "Имтиҳон: Имтиҳони санҷишии ниҳоӣ",
            "Имтиҳон: Истироҳати пурра — мағзро нигоҳ дор",
            "Имтиҳон: Омодагии рӯзи охир — мурори кӯтоҳ",
            "Имтиҳон: Рӯзи имтиҳон — бовар ба худ, муваффақият!",
        ],
        "react" =>
        [
            "React: Насб — Node.js, npm, create-react-app / Vite",
            "React: JSX — HTML дар JavaScript",
            "React: Компонентҳои функсионалӣ",
            "React: Props — додани маълумот байни компонентҳо",
            "React: useState — ҳолати маҳаллӣ",
            "React: useEffect — таъсироти паҳлӯ",
            "React: Рӯйхатҳо ва key prop",
            "React: Идоракунии рӯйдодҳо (onClick, onChange)",
            "React: Шартии рендеринг (conditional rendering)",
            "React: Формҳо ва controlled components",
            "React: useContext — маълумоти умумӣ",
            "React: useReducer — state-и мураккаб",
            "React: useMemo ва useCallback — оптимизатсия",
            "React: useRef — ишора ба DOM элемент",
            "React: React Router — навигатсия",
            "React: Fetch ва async/await дар React",
            "React: TanStack Query — server state",
            "React: Zustand — state management",
            "React: Tailwind CSS бо React",
            "React: TypeScript бо React",
            "React: Тестҳо — React Testing Library",
            "React: Next.js — асосҳо",
            "React: Next.js — App Router",
            "React: Next.js — Server Components",
            "React: Next.js — API Routes",
            "React: Deployment — Vercel",
            "React: Error Boundaries",
            "React: Performance — Profiler",
            "React: Accessibility (a11y)",
            "React: Лоиҳаи ниҳоӣ — Full-stack Next.js App",
        ],
        _ => Enumerable.Range(1, 90).Select(d => $"{label}: Дарси {d}").ToList()
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
                "Медитатсия ва нафаскашии чуқур",
                "10 дақиқа медитатсия барои оромиши зеҳн ва коҳиши стресс.",
                ToUtcFromLocal(date, 7, 30, userTimeZone),
                10,
                "Тадқиқотҳо нишон медиҳанд ки медитатсияи рӯзона маҳсулнокиро 23% зиёд мекунад."),

            new GeminiSuggestedTask(
                "Хондани китоб ё мақолаи касбӣ",
                "30 дақиқа хондан барои рушди касбӣ ва афзоиши дониш.",
                ToUtcFromLocal(date, 21, 0, userTimeZone),
                30,
                "Роҳбарони муваффақ рӯзона ҳадди ақал 30 дақиқа мехонанд."),

            new GeminiSuggestedTask(
                "Нӯшидани об ва ҳаракати кӯтоҳ",
                "Ҳар 2 соат 5 дақиқа ҳаракат кун ва як стакан об нӯш.",
                ToUtcFromLocal(date, 11, 0, userTimeZone),
                5,
                "Нӯшидани 8 стакан об дар рӯз энергия ва тамаркузро беҳтар мекунад."),

            new GeminiSuggestedTask(
                "Банақшагирии рӯзи оянда",
                "10 дақиқа барои навиштани корҳои муҳими фардо ва таъини авлавиятҳо.",
                ToUtcFromLocal(date, 21, 30, userTimeZone),
                10,
                "Банақшагирии шабона вақти саҳариро то 1 соат кам мекунад."),

            new GeminiSuggestedTask(
                "Вақти сифатнок бо оила",
                "30 дақиқа бидуни телефон бо оила суҳбат кун ё бозӣ кун.",
                ToUtcFromLocal(date, 19, 30, userTimeZone),
                30,
                "Муносибатҳои қавии оилавӣ асоси хушбахтӣ ва мотиватсияи рӯзмарра мебошанд.")
        ];
    }

    private static List<GeminiScheduledTask> ParseTasksFromText(string text, DateTime date, string? userTimeZone = null)
    {
        var tasks = new List<GeminiScheduledTask>();

        // Matches: "соати 8:00", "соати 10-10:30", "соати 10:30-12:00", "Саҳар соати 8"
        var pattern = @"[Сс]оати\s+(\d{1,2}(?::\d{2})?)(?:\s*[-–]\s*(\d{1,2}(?::\d{2})?))?(.+?)(?=\.\s*[А-ЯҲҶӢОУа-яҳҷӣоу]|[Сс]оати|\z)";
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
                "Корҳои рӯзона",
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

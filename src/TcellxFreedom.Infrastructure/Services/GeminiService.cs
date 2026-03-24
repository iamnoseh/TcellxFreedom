using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TcellxFreedom.Application.DTOs.Gemini;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Infrastructure.Configuration;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class GeminiService(IHttpClientFactory httpClientFactory, IOptions<GeminiSettings> options)
    : IGeminiService
{
    private readonly GeminiSettings _settings = options.Value;
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
            var scheduledAt = request.StartDate.Date.AddDays(dayOffset).AddHours(hour);

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
            return ParseChatScheduleResult(responseText, request.Date);
        }
        catch
        {
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

    private async Task<string> CallGeminiAsync(object requestBody, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Gemini");
        var url = $"/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(ct);

            var doc = JsonNode.Parse(body);
            var text = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
            return text ?? throw new InvalidOperationException("Empty response from Gemini.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("AI service temporarily unavailable.", ex);
        }
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
        var userMessage = JsonSerializer.Serialize(new
        {
            instruction = "Корбар матни озод навиштааст ки дар он рӯзнома ва корҳои рӯзонаашро тавсиф кардааст. Вазифаи ту: 1) Ҳамаи корҳо ва вақтҳои зикршударо аз матн кашида гир ва ба вазифаҳои сохтёфта табдил деҳ. 2) Барои ҳар вазифа вақти мушаххас таъин кун. 3) Дақиқан 5 вазифаи иловагии муфид пешниҳод кун ки ба зиндагии корбар арзиши иловагӣ медиҳанд. 4) Унвон ва тавсифи нақшаро тартиб деҳ. Ҳамаи матнҳо бояд ба забони ТОҶИКӢ бошанд. Вақтҳо дар формати UTC ISO8601.",
            date = request.Date.ToString("yyyy-MM-dd"),
            userTimeZone = request.UserTimeZone,
            userText = request.FreeText,
            responseSchema = new
            {
                planTitle = "унвони нақша (ба тоҷикӣ)",
                planDescription = "тавсифи кӯтоҳи нақша (ба тоҷикӣ)",
                scheduledTasks = new[]
                {
                    new { title = "сатр", description = "сатр ё null", scheduledAtUtc = "ISO8601", estimatedMinutes = 30, rationale = "сатр ё null", recurrence = "None" }
                },
                suggestedAdditionalTasks = new[]
                {
                    new { title = "сатр", description = "сатр ё null", scheduledAtUtc = "ISO8601", estimatedMinutes = 30, rationale = "сабаби пешниҳод (ҳатмист)" }
                }
            }
        });

        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = "Ту мутахассиси банақшагирии шахсӣ ҳастӣ. Матни озоди корбарро таҳлил кун, корҳои ӯро кашида гир, ҷадвал тартиб деҳ ва вазифаҳои иловагии муфид пешниҳод кун. Дақиқан 5 вазифаи иловагӣ лозим аст. Ҳамаи матнҳо ба забони ТОҶИКӢ. Фақат JSON баргардон." } }
            },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
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
        var tasks = ParseTasksFromText(request.FreeText, request.Date);
        var suggestions = BuildDefaultSuggestions(request.Date);
        return new GeminiChatScheduleResult(
            "Нақшаи рӯзона",
            "Нақша тибқи матни корбар тартиб дода шуд.",
            tasks,
            suggestions);
    }

    private static List<GeminiSuggestedTask> BuildDefaultSuggestions(DateTime date)
    {
        return
        [
            new GeminiSuggestedTask(
                "Медитатсия ва нафаскашии чуқур",
                "10 дақиқа медитатсия барои оромиши зеҳн ва коҳиши стресс.",
                date.Date.AddHours(7).AddMinutes(30),
                10,
                "Тадқиқотҳо нишон медиҳанд ки медитатсияи рӯзона маҳсулнокиро 23% зиёд мекунад."),

            new GeminiSuggestedTask(
                "Хондани китоб ё мақолаи касбӣ",
                "30 дақиқа хондан барои рушди касбӣ ва афзоиши дониш.",
                date.Date.AddHours(21),
                30,
                "Роҳбарони муваффақ рӯзона ҳадди ақал 30 дақиқа мехонанд."),

            new GeminiSuggestedTask(
                "Нӯшидани об ва ҳаракати кӯтоҳ",
                "Ҳар 2 соат 5 дақиқа ҳаракат кун ва як стакан об нӯш.",
                date.Date.AddHours(11),
                5,
                "Нӯшидани 8 стакан об дар рӯз энергия ва тамаркузро беҳтар мекунад."),

            new GeminiSuggestedTask(
                "Банақшагирии рӯзи оянда",
                "10 дақиқа барои навиштани корҳои муҳими фардо ва таъини авлавиятҳо.",
                date.Date.AddHours(21).AddMinutes(30),
                10,
                "Банақшагирии шабона вақти саҳариро то 1 соат кам мекунад."),

            new GeminiSuggestedTask(
                "Вақти сифатнок бо оила",
                "30 дақиқа бидуни телефон бо оила суҳбат кун ё бозӣ кун.",
                date.Date.AddHours(19).AddMinutes(30),
                30,
                "Муносибатҳои қавии оилавӣ асоси хушбахтӣ ва мотиватсияи рӯзмарра мебошанд.")
        ];
    }

    private static List<GeminiScheduledTask> ParseTasksFromText(string text, DateTime date)
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

            var scheduledAt = date.Date.AddHours(startH).AddMinutes(startM);

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
                date.Date.AddHours(9),
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

namespace TcellxFreedom.Application.DTOs.Gemini;

public sealed record GeminiStatsRequest(
    string UserId,
    List<WeeklyStatSummary> RecentWeeks
);

public sealed record WeeklyStatSummary(
    DateTime WeekStart,
    decimal CompletionRate,
    int TotalTasks,
    int CompletedTasks
);

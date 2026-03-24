namespace TcellxFreedom.Application.DTOs.Statistics;

public sealed record StatisticsDto(
    List<WeeklyStatDto> WeeklyStats,
    List<string> AiSuggestions
);

public sealed record WeeklyStatDto(
    DateTime WeekStart,
    int TotalTasks,
    int CompletedTasks,
    int SkippedTasks,
    decimal CompletionRate
);

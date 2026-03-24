using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Application.DTOs.Gemini;

public sealed record GeminiScheduleResult(
    List<GeminiScheduledTask> ScheduledTasks,
    List<GeminiSuggestedTask> SuggestedAdditionalTasks
);

public sealed record GeminiChatScheduleResult(
    string PlanTitle,
    string? PlanDescription,
    List<GeminiScheduledTask> ScheduledTasks,
    List<GeminiSuggestedTask> SuggestedAdditionalTasks
);

public sealed record GeminiScheduledTask(
    string Title,
    string? Description,
    DateTime ScheduledAt,
    int EstimatedMinutes,
    string? Rationale,
    RecurrenceType Recurrence
);

public sealed record GeminiSuggestedTask(
    string Title,
    string? Description,
    DateTime ScheduledAt,
    int EstimatedMinutes,
    string Rationale
);

namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record PlanTaskDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime ScheduledAt,
    int EstimatedMinutes,
    string Status,
    bool IsAiSuggested,
    bool IsAccepted,
    string Recurrence,
    string? AiRationale
);

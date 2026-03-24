namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record AddQuickTaskRequestDto(
    string Title,
    string? Description,
    DateTime ScheduledAt,
    int EstimatedMinutes = 60
);

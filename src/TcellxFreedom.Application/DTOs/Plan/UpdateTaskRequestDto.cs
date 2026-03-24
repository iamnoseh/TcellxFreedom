namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record UpdateTaskRequestDto(
    string? Title,
    string? Description,
    DateTime? ScheduledAt,
    int? EstimatedMinutes
);

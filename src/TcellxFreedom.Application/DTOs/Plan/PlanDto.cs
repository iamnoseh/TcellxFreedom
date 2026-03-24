namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record PlanDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int TotalTasks,
    int CompletedTasks,
    DateTime CreatedAt
);

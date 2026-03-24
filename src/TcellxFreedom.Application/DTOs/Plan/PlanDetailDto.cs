namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record PlanDetailDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    List<PlanTaskDto> Tasks,
    DateTime CreatedAt
);

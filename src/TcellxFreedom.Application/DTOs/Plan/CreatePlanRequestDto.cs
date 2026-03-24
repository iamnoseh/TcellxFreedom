namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record CreatePlanRequestDto(
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string UserTimeZone,
    List<TaskInputDto> Tasks
);

public sealed record TaskInputDto(
    string Title,
    string? Description,
    string? PreferredTimeOfDay,
    string Recurrence
);

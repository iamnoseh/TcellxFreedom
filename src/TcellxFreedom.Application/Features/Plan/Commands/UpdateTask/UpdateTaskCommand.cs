using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    string UserId,
    Guid TaskId,
    string? Title,
    string? Description,
    DateTime? ScheduledAt,
    int? EstimatedMinutes
) : IRequest<Response<PlanTaskDto>>;

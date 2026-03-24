using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.RescheduleTask;

public sealed record RescheduleTaskCommand(
    string UserId,
    Guid TaskId,
    DateTime NewScheduledAt
) : IRequest<Response<PlanTaskDto>>;

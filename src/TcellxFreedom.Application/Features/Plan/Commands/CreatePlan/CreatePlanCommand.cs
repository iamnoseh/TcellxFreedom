using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.CreatePlan;

public sealed record CreatePlanCommand(
    string UserId,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string UserTimeZone,
    List<TaskInputDto> Tasks
) : IRequest<Response<PlanDetailDto>>;

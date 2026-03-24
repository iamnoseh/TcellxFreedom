using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetPlanById;

public sealed record GetPlanByIdQuery(string UserId, Guid PlanId) : IRequest<Response<PlanDetailDto>>;

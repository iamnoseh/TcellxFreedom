using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetPlans;

public sealed record GetPlansQuery(string UserId) : IRequest<Response<List<PlanDto>>>;

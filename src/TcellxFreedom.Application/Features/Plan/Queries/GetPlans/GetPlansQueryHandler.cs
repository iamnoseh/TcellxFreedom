using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetPlans;

public sealed class GetPlansQueryHandler(IPlanRepository planRepository)
    : IRequestHandler<GetPlansQuery, Response<List<PlanDto>>>
{
    public async Task<Response<List<PlanDto>>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await planRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return new Response<List<PlanDto>>(plans.Select(p => p.ToPlanDto()).ToList());
    }
}

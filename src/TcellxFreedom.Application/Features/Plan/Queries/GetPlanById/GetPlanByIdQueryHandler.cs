using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetPlanById;

public sealed class GetPlanByIdQueryHandler(IPlanRepository planRepository)
    : IRequestHandler<GetPlanByIdQuery, Response<PlanDetailDto>>
{
    public async Task<Response<PlanDetailDto>> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdWithTasksAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return new Response<PlanDetailDto>(HttpStatusCode.NotFound, "Накша ёфт нашуд.");
        if (plan.UserId != request.UserId)
            return new Response<PlanDetailDto>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст.");

        return new Response<PlanDetailDto>(plan.ToDetailDto());
    }
}

using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetAiSuggestions;

public sealed class GetAiSuggestionsQueryHandler(IPlanRepository planRepository)
    : IRequestHandler<GetAiSuggestionsQuery, Response<List<PlanTaskDto>>>
{
    public async Task<Response<List<PlanTaskDto>>> Handle(GetAiSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdWithTasksAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return new Response<List<PlanTaskDto>>(HttpStatusCode.NotFound, "План не найден.");
        if (plan.UserId != request.UserId)
            return new Response<List<PlanTaskDto>>(HttpStatusCode.Forbidden, "Доступ запрещён.");

        var suggestions = plan.Tasks
            .Where(t => t.IsAiSuggested)
            .Select(t => t.ToDto())
            .ToList();

        return new Response<List<PlanTaskDto>>(suggestions);
    }
}

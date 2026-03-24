using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetAiSuggestions;

public sealed record GetAiSuggestionsQuery(string UserId, Guid PlanId)
    : IRequest<Response<List<PlanTaskDto>>>;

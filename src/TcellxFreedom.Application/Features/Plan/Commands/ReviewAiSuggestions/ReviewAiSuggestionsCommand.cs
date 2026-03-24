using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.ReviewAiSuggestions;

public sealed record ReviewAiSuggestionsCommand(
    string UserId,
    Guid PlanId,
    List<TaskAcceptanceDto> Decisions
) : IRequest<Response<PlanDetailDto>>;

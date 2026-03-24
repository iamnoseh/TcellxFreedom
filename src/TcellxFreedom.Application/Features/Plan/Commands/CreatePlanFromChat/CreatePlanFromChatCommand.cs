using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.CreatePlanFromChat;

public sealed record CreatePlanFromChatCommand(
    string UserId,
    string Text,
    DateTime Date,
    string UserTimeZone
) : IRequest<Response<PlanDetailDto>>;

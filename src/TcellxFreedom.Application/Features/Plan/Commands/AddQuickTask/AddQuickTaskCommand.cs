using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.AddQuickTask;

public sealed record AddQuickTaskCommand(
    string UserId,
    string Title,
    string? Description,
    DateTime ScheduledAt,
    int EstimatedMinutes
) : IRequest<Response<PlanDetailDto>>;

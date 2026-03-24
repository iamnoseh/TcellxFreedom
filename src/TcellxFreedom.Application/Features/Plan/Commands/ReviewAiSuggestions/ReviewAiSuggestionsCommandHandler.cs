using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Commands.ReviewAiSuggestions;

public sealed class ReviewAiSuggestionsCommandHandler(
    IPlanRepository planRepository,
    INotificationService notificationService)
    : IRequestHandler<ReviewAiSuggestionsCommand, Response<PlanDetailDto>>
{
    public async Task<Response<PlanDetailDto>> Handle(ReviewAiSuggestionsCommand request, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdWithTasksAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return new Response<PlanDetailDto>(HttpStatusCode.NotFound, "Накша ёфт нашуд.");
        if (plan.UserId != request.UserId)
            return new Response<PlanDetailDto>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст.");

        var decisionMap = request.Decisions.ToDictionary(d => d.TaskId, d => d.Accept);

        foreach (var task in plan.Tasks.Where(t => t.IsAiSuggested))
        {
            if (decisionMap.TryGetValue(task.Id, out var accept) && accept)
                task.Accept();
            else
                task.Reject(); // дар decisions нагузошта ё рад — автоматик reject
        }

        plan.Activate();
        await planRepository.UpdateAsync(plan, cancellationToken);

        foreach (var task in plan.Tasks.Where(t => t.IsAccepted && t.Status != Domain.Enums.TaskStatus.Skipped))
            await notificationService.ScheduleForTaskAsync(task, plan.UserId, cancellationToken);

        return new Response<PlanDetailDto>(plan.ToDetailDto());
    }
}

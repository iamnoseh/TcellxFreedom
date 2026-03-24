using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Gemini;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using DomainPlan = TcellxFreedom.Domain.Entities.Plan;
using DomainPlanTask = TcellxFreedom.Domain.Entities.PlanTask;

namespace TcellxFreedom.Application.Features.Plan.Commands.CreatePlan;

public sealed class CreatePlanCommandHandler(
    IPlanRepository planRepository,
    IGeminiService geminiService)
    : IRequestHandler<CreatePlanCommand, Response<PlanDetailDto>>
{
    public async Task<Response<PlanDetailDto>> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var geminiRequest = BuildGeminiRequest(request);
        var geminiResult = await geminiService.ScheduleTasksAsync(geminiRequest, cancellationToken);

        var plan = DomainPlan.Create(request.UserId, request.Title, request.Description, request.StartDate, request.EndDate);

        foreach (var scheduled in geminiResult.ScheduledTasks)
        {
            var task = DomainPlanTask.Create(plan.Id, scheduled.Title, scheduled.Description,
                scheduled.ScheduledAt, scheduled.EstimatedMinutes, isAiSuggested: false, scheduled.Recurrence);
            if (scheduled.Rationale is not null) task.SetAiRationale(scheduled.Rationale);
            plan.AddTask(task);
        }

        foreach (var suggested in geminiResult.SuggestedAdditionalTasks)
        {
            var task = DomainPlanTask.Create(plan.Id, suggested.Title, suggested.Description,
                suggested.ScheduledAt, suggested.EstimatedMinutes, isAiSuggested: true);
            task.SetAiRationale(suggested.Rationale);
            plan.AddTask(task);
        }

        await planRepository.CreateAsync(plan, cancellationToken);
        return new Response<PlanDetailDto>(plan.ToDetailDto());
    }

    private static GeminiScheduleRequest BuildGeminiRequest(CreatePlanCommand request) => new(
        request.UserTimeZone,
        request.StartDate,
        request.EndDate,
        request.Tasks.Select(t => new GeminiTaskInput(
            t.Title,
            t.Description,
            t.PreferredTimeOfDay,
            Enum.TryParse<RecurrenceType>(t.Recurrence, true, out var rec) ? rec : RecurrenceType.None
        )).ToList());
}

using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Gemini;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;
using DomainPlan = TcellxFreedom.Domain.Entities.Plan;
using DomainPlanTask = TcellxFreedom.Domain.Entities.PlanTask;

namespace TcellxFreedom.Application.Features.Plan.Commands.CreatePlanFromChat;

public sealed class CreatePlanFromChatCommandHandler(
    IPlanRepository planRepository,
    IGeminiService geminiService)
    : IRequestHandler<CreatePlanFromChatCommand, Response<PlanDetailDto>>
{
    public async Task<Response<PlanDetailDto>> Handle(CreatePlanFromChatCommand request, CancellationToken cancellationToken)
    {
        var geminiRequest = new GeminiChatScheduleRequest(request.Text, request.Date, request.UserTimeZone);
        var geminiResult = await geminiService.ParseAndScheduleFromChatAsync(geminiRequest, cancellationToken);

        var startDate = request.Date.Date;
        var endDate = request.Date.Date.AddHours(23).AddMinutes(59);

        var plan = DomainPlan.Create(request.UserId, geminiResult.PlanTitle, geminiResult.PlanDescription, startDate, endDate);

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
}

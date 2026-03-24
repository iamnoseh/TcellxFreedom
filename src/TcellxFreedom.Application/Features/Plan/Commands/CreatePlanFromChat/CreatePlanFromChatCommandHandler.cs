using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Gemini;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Application.Features.Plan.Commands.AddQuickTask;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using DomainPlan = TcellxFreedom.Domain.Entities.Plan;
using DomainPlanTask = TcellxFreedom.Domain.Entities.PlanTask;

namespace TcellxFreedom.Application.Features.Plan.Commands.CreatePlanFromChat;

public sealed class CreatePlanFromChatCommandHandler(
    IPlanRepository planRepository,
    IPlanTaskRepository planTaskRepository,
    IGeminiService geminiService)
    : IRequestHandler<CreatePlanFromChatCommand, Response<PlanDetailDto>>
{
    public async Task<Response<PlanDetailDto>> Handle(CreatePlanFromChatCommand request, CancellationToken cancellationToken)
    {
        var geminiRequest = new GeminiChatScheduleRequest(request.Text, request.Date, request.UserTimeZone, request.EndDate);
        var geminiResult = await geminiService.ParseAndScheduleFromChatAsync(geminiRequest, cancellationToken);

        var startDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        var endDate = request.EndDate.HasValue
            ? DateTime.SpecifyKind(request.EndDate.Value.Date, DateTimeKind.Utc).AddHours(23).AddMinutes(59)
            : startDate.AddHours(23).AddMinutes(59);

        // Load all existing tasks in the date range once (for conflict detection)
        var existingTasks = await planTaskRepository.GetByUserAndDateRangeAsync(
            request.UserId, startDate, endDate, cancellationToken);

        // In-memory registry of already-planned tasks in THIS plan (per day)
        // so we also avoid intra-plan conflicts
        var inPlanTasks = new List<PlanTask>();

        var plan = DomainPlan.Create(request.UserId, geminiResult.PlanTitle, geminiResult.PlanDescription, startDate, endDate);

        bool aiReturnedCurriculum = request.EndDate.HasValue && geminiResult.ScheduledTasks.Count > 1;

        foreach (var scheduled in geminiResult.ScheduledTasks)
        {
            if (aiReturnedCurriculum)
            {
                // AI returned a full curriculum — one unique task per day, resolve conflicts
                var resolvedAt = ResolveForDay(scheduled.ScheduledAt, scheduled.EstimatedMinutes, existingTasks, inPlanTasks);
                var task = DomainPlanTask.Create(plan.Id, scheduled.Title, scheduled.Description,
                    resolvedAt, scheduled.EstimatedMinutes, isAiSuggested: false, RecurrenceType.None);
                if (scheduled.Rationale is not null) task.SetAiRationale(scheduled.Rationale);
                plan.AddTask(task);
                inPlanTasks.Add(task);
            }
            else if (request.EndDate.HasValue)
            {
                // Fallback: expand single task to every day in range
                var timeOfDay = scheduled.ScheduledAt.TimeOfDay;
                var current = startDate.Date;
                while (current <= endDate.Date)
                {
                    var desired = DateTime.SpecifyKind(current + timeOfDay, DateTimeKind.Utc);
                    var resolvedAt = ResolveForDay(desired, scheduled.EstimatedMinutes, existingTasks, inPlanTasks);
                    var dailyTask = DomainPlanTask.Create(plan.Id, scheduled.Title, scheduled.Description,
                        resolvedAt, scheduled.EstimatedMinutes, isAiSuggested: false, RecurrenceType.None);
                    if (scheduled.Rationale is not null) dailyTask.SetAiRationale(scheduled.Rationale);
                    plan.AddTask(dailyTask);
                    inPlanTasks.Add(dailyTask);
                    current = current.AddDays(1);
                }
            }
            else
            {
                // Single-day plan
                var resolvedAt = ResolveForDay(scheduled.ScheduledAt, scheduled.EstimatedMinutes, existingTasks, inPlanTasks);
                var task = DomainPlanTask.Create(plan.Id, scheduled.Title, scheduled.Description,
                    resolvedAt, scheduled.EstimatedMinutes, isAiSuggested: false, RecurrenceType.None);
                if (scheduled.Rationale is not null) task.SetAiRationale(scheduled.Rationale);
                plan.AddTask(task);
                inPlanTasks.Add(task);
            }
        }

        await planRepository.CreateAsync(plan, cancellationToken);
        return new Response<PlanDetailDto>(plan.ToDetailDto());
    }

    /// <summary>
    /// Resolves a conflict for a given day by considering both existing DB tasks
    /// and tasks already added in the current plan.
    /// </summary>
    private static DateTime ResolveForDay(
        DateTime desired, int durationMinutes,
        IEnumerable<PlanTask> existingDb,
        IEnumerable<PlanTask> inPlan)
    {
        // Only consider tasks on the same calendar day
        var dayTasks = existingDb
            .Concat(inPlan)
            .Where(t => t.ScheduledAt.Date == desired.Date);

        return AddQuickTaskCommandHandler.ResolveConflict(desired, durationMinutes, dayTasks);
    }
}

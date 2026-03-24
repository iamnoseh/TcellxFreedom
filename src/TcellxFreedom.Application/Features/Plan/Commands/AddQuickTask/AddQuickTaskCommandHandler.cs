using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using DomainPlan = TcellxFreedom.Domain.Entities.Plan;
using DomainPlanTask = TcellxFreedom.Domain.Entities.PlanTask;

namespace TcellxFreedom.Application.Features.Plan.Commands.AddQuickTask;

public sealed class AddQuickTaskCommandHandler(
    IPlanRepository planRepository,
    IPlanTaskRepository planTaskRepository)
    : IRequestHandler<AddQuickTaskCommand, Response<PlanDetailDto>>
{
    public async Task<Response<PlanDetailDto>> Handle(AddQuickTaskCommand request, CancellationToken cancellationToken)
    {
        var scheduledAt = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc);

        // Load all existing tasks for the user on the same day to detect conflicts
        var dayStart = DateTime.SpecifyKind(scheduledAt.Date, DateTimeKind.Utc);
        var dayEnd   = dayStart.AddHours(23).AddMinutes(59);
        var existing = await planTaskRepository.GetByUserAndDateRangeAsync(
            request.UserId, dayStart, dayEnd, cancellationToken);

        // Shift scheduledAt forward until no overlap
        var resolvedAt = ResolveConflict(scheduledAt, request.EstimatedMinutes, existing);

        var startDate = dayStart;
        var endDate   = dayEnd;

        var plan = DomainPlan.Create(request.UserId, request.Title, request.Description, startDate, endDate);
        var task = DomainPlanTask.Create(
            plan.Id,
            request.Title,
            request.Description,
            resolvedAt,
            request.EstimatedMinutes,
            isAiSuggested: false,
            RecurrenceType.None);

        plan.AddTask(task);
        await planRepository.CreateAsync(plan, cancellationToken);
        return new Response<PlanDetailDto>(plan.ToDetailDto());
    }

    /// <summary>
    /// Shifts <paramref name="desired"/> forward until it no longer overlaps
    /// with any task in <paramref name="existing"/>.
    /// </summary>
    public static DateTime ResolveConflict(DateTime desired, int durationMinutes, IEnumerable<PlanTask> existing)
    {
        var sorted = existing.OrderBy(t => t.ScheduledAt).ToList();
        var start = desired;
        bool moved;
        do
        {
            moved = false;
            foreach (var t in sorted)
            {
                var tEnd   = t.ScheduledAt.AddMinutes(t.EstimatedMinutes);
                var newEnd = start.AddMinutes(durationMinutes);
                // Overlap: [start, newEnd) intersects [t.ScheduledAt, tEnd)
                if (t.ScheduledAt < newEnd && tEnd > start)
                {
                    start = tEnd; // push to right after the conflicting task
                    moved = true;
                    break;
                }
            }
        } while (moved);

        return start;
    }
}

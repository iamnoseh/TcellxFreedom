using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Infrastructure.Jobs;

public sealed class RecurringTaskGeneratorJob(
    IPlanTaskRepository taskRepository,
    IPlanRepository planRepository,
    INotificationService notificationService)
{
    public async Task ExecuteAsync()
    {
        var recurringTasks = await taskRepository.GetPendingRecurringTasksAsync();

        foreach (var completedTask in recurringTasks.Where(t => t.RecurrenceIntervalDays.HasValue))
        {
            var plan = await planRepository.GetByIdAsync(completedTask.PlanId);
            if (plan?.Status != PlanStatus.Active) continue;

            var nextScheduledAt = completedTask.ScheduledAt.AddDays(completedTask.RecurrenceIntervalDays!.Value);
            if (nextScheduledAt > plan.EndDate) continue;

            var existingTasks = await taskRepository.GetByPlanIdAsync(completedTask.PlanId);
            var alreadyExists = existingTasks.Any(t =>
                t.ParentTaskId == completedTask.Id &&
                t.ScheduledAt.Date == nextScheduledAt.Date);
            if (alreadyExists) continue;

            var newTask = PlanTask.Create(
                completedTask.PlanId,
                completedTask.Title,
                completedTask.Description,
                nextScheduledAt,
                completedTask.EstimatedMinutes,
                isAiSuggested: false,
                completedTask.Recurrence,
                completedTask.Id);

            await taskRepository.AddRangeAsync([newTask]);
            await notificationService.ScheduleForTaskAsync(newTask, plan.UserId);
        }
    }
}

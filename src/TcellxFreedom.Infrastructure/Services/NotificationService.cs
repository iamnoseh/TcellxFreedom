using Microsoft.Extensions.Options;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Configuration;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class NotificationService(
    ITaskNotificationRepository notificationRepository,
    INotificationScheduler notificationScheduler,
    IOptions<NotificationSettings> options)
    : INotificationService
{
    private readonly NotificationSettings _settings = options.Value;

    public async Task ScheduleForTaskAsync(PlanTask task, string userId, CancellationToken ct = default)
    {
        var offsetMinutes = Random.Shared.Next(_settings.MinOffsetMinutes, _settings.MaxOffsetMinutes + 1);
        var notifyAt = task.ScheduledAt.AddMinutes(-offsetMinutes);
        if (notifyAt <= DateTime.UtcNow) return;

        var notification = TaskNotification.Create(
            task.Id,
            userId,
            notifyAt,
            $"Ёдоварӣ: {task.Title}",
            $"Вазифаи «{task.Title}» баъди {offsetMinutes} дақиқа оғоз мешавад.");

        await notificationRepository.CreateAsync(notification, ct);

        var jobId = await notificationScheduler.ScheduleAsync(notification.Id, notification.ScheduledAt, ct);
        notification.SetHangfireJobId(jobId);
        await notificationRepository.UpdateAsync(notification, ct);
    }

    public async Task CancelForTaskAsync(Guid planTaskId, CancellationToken ct = default)
    {
        var notification = await notificationRepository.GetByPlanTaskIdAsync(planTaskId, ct);
        if (notification?.HangfireJobId is null) return;

        notification.Cancel();
        await notificationScheduler.CancelAsync(notification.HangfireJobId, ct);
        await notificationRepository.UpdateAsync(notification, ct);
    }
}

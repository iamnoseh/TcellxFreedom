using Hangfire;
using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class NotificationSchedulerService(IBackgroundJobClient backgroundJobClient)
    : INotificationScheduler
{
    public Task<string> ScheduleAsync(Guid notificationId, DateTime fireAt, CancellationToken ct = default)
    {
        var delay = fireAt - DateTime.UtcNow;
        if (delay <= TimeSpan.Zero)
            delay = TimeSpan.Zero;

        var jobId = backgroundJobClient.Schedule<INotificationProcessor>(
            p => p.ProcessAsync(notificationId),
            delay);

        return Task.FromResult(jobId);
    }

    public Task CancelAsync(string hangfireJobId, CancellationToken ct = default)
    {
        backgroundJobClient.Delete(hangfireJobId);
        return Task.CompletedTask;
    }
}

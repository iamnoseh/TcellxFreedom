namespace TcellxFreedom.Application.Interfaces;

public interface INotificationScheduler
{
    Task<string> ScheduleAsync(Guid notificationId, DateTime fireAt, CancellationToken ct = default);
    Task CancelAsync(string hangfireJobId, CancellationToken ct = default);
}

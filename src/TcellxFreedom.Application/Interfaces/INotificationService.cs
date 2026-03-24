using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Application.Interfaces;

public interface INotificationService
{
    Task ScheduleForTaskAsync(PlanTask task, string userId, CancellationToken ct = default);
    Task CancelForTaskAsync(Guid planTaskId, CancellationToken ct = default);
}

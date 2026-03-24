using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Domain.Interfaces;

public interface ITaskNotificationRepository
{
    Task<TaskNotification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<TaskNotification>> GetUpcomingByUserIdAsync(string userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<TaskNotification>> GetPendingNotificationsAsync(CancellationToken ct = default);
    Task<TaskNotification?> GetByPlanTaskIdAsync(Guid planTaskId, CancellationToken ct = default);
    Task CreateAsync(TaskNotification notification, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TaskNotification> notifications, CancellationToken ct = default);
    Task UpdateAsync(TaskNotification notification, CancellationToken ct = default);
}

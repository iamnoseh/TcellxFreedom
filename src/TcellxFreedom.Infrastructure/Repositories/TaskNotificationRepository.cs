using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class TaskNotificationRepository(ApplicationDbContext context) : ITaskNotificationRepository
{
    public Task<TaskNotification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.TaskNotifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task<List<TaskNotification>> GetUpcomingByUserIdAsync(string userId, DateTime from, DateTime to, CancellationToken ct = default)
        => context.TaskNotifications
            .Where(n => n.UserId == userId && n.ScheduledAt >= from && n.ScheduledAt <= to && n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.ScheduledAt)
            .ToListAsync(ct);

    public Task<List<TaskNotification>> GetPendingNotificationsAsync(CancellationToken ct = default)
        => context.TaskNotifications.Where(n => n.Status == NotificationStatus.Pending).ToListAsync(ct);

    public Task<TaskNotification?> GetByPlanTaskIdAsync(Guid planTaskId, CancellationToken ct = default)
        => context.TaskNotifications.FirstOrDefaultAsync(n => n.PlanTaskId == planTaskId, ct);

    public async Task CreateAsync(TaskNotification notification, CancellationToken ct = default)
    {
        context.TaskNotifications.Add(notification);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<TaskNotification> notifications, CancellationToken ct = default)
    {
        context.TaskNotifications.AddRange(notifications);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaskNotification notification, CancellationToken ct = default)
    {
        context.TaskNotifications.Update(notification);
        await context.SaveChangesAsync(ct);
    }
}

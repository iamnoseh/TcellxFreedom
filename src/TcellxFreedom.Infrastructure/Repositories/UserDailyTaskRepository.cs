using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class UserDailyTaskRepository(ApplicationDbContext context) : IUserDailyTaskRepository
{
    public Task<List<UserDailyTask>> GetByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default)
        => context.UserDailyTasks
            .Include(t => t.Template)
            .Where(t => t.UserId == userId && t.AssignedDate == date)
            .ToListAsync(ct);

    public Task<UserDailyTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.UserDailyTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<UserDailyTask?> GetByIdWithTemplateAsync(Guid id, CancellationToken ct = default)
        => context.UserDailyTasks
            .Include(t => t.Template)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddRangeAsync(List<UserDailyTask> tasks, CancellationToken ct = default)
    {
        context.UserDailyTasks.AddRange(tasks);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserDailyTask task, CancellationToken ct = default)
    {
        context.UserDailyTasks.Update(task);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(List<UserDailyTask> tasks, CancellationToken ct = default)
    {
        context.UserDailyTasks.UpdateRange(tasks);
        await context.SaveChangesAsync(ct);
    }

    public Task<List<UserDailyTask>> GetPendingByDateAsync(DateOnly date, CancellationToken ct = default)
        => context.UserDailyTasks
            .Where(t => t.AssignedDate == date && t.Status == DailyTaskStatus.Pending)
            .ToListAsync(ct);

    public Task<int> CountCompletedByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default)
        => context.UserDailyTasks
            .CountAsync(t => t.UserId == userId && t.AssignedDate == date && t.Status == DailyTaskStatus.Completed, ct);

    public Task<int> CountByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default)
        => context.UserDailyTasks
            .CountAsync(t => t.UserId == userId && t.AssignedDate == date, ct);
}

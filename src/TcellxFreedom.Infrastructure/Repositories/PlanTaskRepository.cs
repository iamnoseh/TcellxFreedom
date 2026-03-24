using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;
using TaskStatus = TcellxFreedom.Domain.Enums.TaskStatus;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class PlanTaskRepository(ApplicationDbContext context) : IPlanTaskRepository
{
    public Task<PlanTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.PlanTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<PlanTask>> GetByPlanIdAsync(Guid planId, CancellationToken ct = default)
        => context.PlanTasks.Where(t => t.PlanId == planId).ToListAsync(ct);

    public Task<List<PlanTask>> GetByUserAndDateRangeAsync(string userId, DateTime from, DateTime to, CancellationToken ct = default)
        => context.Plans
            .Where(p => p.UserId == userId)
            .SelectMany(p => p.Tasks)
            .Where(t => t.ScheduledAt >= from && t.ScheduledAt <= to)
            .ToListAsync(ct);

    public Task<List<PlanTask>> GetAllByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => context.PlanTasks
            .Where(t => t.ScheduledAt >= from && t.ScheduledAt <= to)
            .ToListAsync(ct);

    public Task<List<PlanTask>> GetPendingRecurringTasksAsync(CancellationToken ct = default)
        => context.PlanTasks
            .Where(t => t.Recurrence != RecurrenceType.None && t.Status == TaskStatus.Completed)
            .ToListAsync(ct);

    public async Task UpdateAsync(PlanTask task, CancellationToken ct = default)
    {
        context.PlanTasks.Update(task);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<PlanTask> tasks, CancellationToken ct = default)
    {
        context.PlanTasks.AddRange(tasks);
        await context.SaveChangesAsync(ct);
    }
}

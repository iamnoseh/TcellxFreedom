using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class UserTaskStatisticRepository(ApplicationDbContext context) : IUserTaskStatisticRepository
{
    public Task<List<UserTaskStatistic>> GetByUserIdAsync(string userId, int weekCount, CancellationToken ct = default)
        => context.UserTaskStatistics
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.WeekStartDate)
            .Take(weekCount)
            .ToListAsync(ct);

    public Task<UserTaskStatistic?> GetByUserAndWeekAsync(string userId, DateTime weekStart, CancellationToken ct = default)
        => context.UserTaskStatistics
            .FirstOrDefaultAsync(s => s.UserId == userId && s.WeekStartDate == weekStart.Date, ct);

    public async Task CreateAsync(UserTaskStatistic stat, CancellationToken ct = default)
    {
        context.UserTaskStatistics.Add(stat);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserTaskStatistic stat, CancellationToken ct = default)
    {
        context.UserTaskStatistics.Update(stat);
        await context.SaveChangesAsync(ct);
    }
}

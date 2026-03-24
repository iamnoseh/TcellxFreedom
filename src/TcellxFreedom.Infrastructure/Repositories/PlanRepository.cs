using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class PlanRepository(ApplicationDbContext context) : IPlanRepository
{
    public Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Plan?> GetByIdWithTasksAsync(Guid id, CancellationToken ct = default)
        => context.Plans.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<Plan>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => context.Plans.Include(p => p.Tasks).Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).ToListAsync(ct);

    public async Task<Plan> CreateAsync(Plan plan, CancellationToken ct = default)
    {
        context.Plans.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan;
    }

    public async Task UpdateAsync(Plan plan, CancellationToken ct = default)
    {
        context.Plans.Update(plan);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await GetByIdAsync(id, ct);
        if (plan is not null)
        {
            context.Plans.Remove(plan);
            await context.SaveChangesAsync(ct);
        }
    }
}

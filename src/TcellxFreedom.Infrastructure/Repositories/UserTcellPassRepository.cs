using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class UserTcellPassRepository(ApplicationDbContext context) : IUserTcellPassRepository
{
    public Task<UserTcellPass?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => context.UserTcellPasses.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<List<UserTcellPass>> GetAllActiveAsync(CancellationToken ct = default)
        => context.UserTcellPasses.ToListAsync(ct);

    public Task<List<UserTcellPass>> GetTopByXpAsync(int topN = 50, CancellationToken ct = default)
        => context.UserTcellPasses.OrderByDescending(p => p.TotalXp).Take(topN).ToListAsync(ct);

    public async Task<UserTcellPass> CreateAsync(UserTcellPass pass, CancellationToken ct = default)
    {
        context.UserTcellPasses.Add(pass);
        await context.SaveChangesAsync(ct);
        return pass;
    }

    public async Task UpdateAsync(UserTcellPass pass, CancellationToken ct = default)
    {
        context.UserTcellPasses.Update(pass);
        await context.SaveChangesAsync(ct);
    }
}

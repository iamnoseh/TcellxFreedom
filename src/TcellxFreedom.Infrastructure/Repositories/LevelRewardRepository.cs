using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class LevelRewardRepository(ApplicationDbContext context) : ILevelRewardRepository
{
    public Task<List<LevelReward>> GetAllAsync(CancellationToken ct = default)
        => context.LevelRewards.OrderBy(r => r.Level).ThenBy(r => r.Tier).ToListAsync(ct);

    public Task<LevelReward?> GetByLevelAndTierAsync(int level, UserTier tier, CancellationToken ct = default)
        => context.LevelRewards.FirstOrDefaultAsync(r => r.Level == level && r.Tier == tier, ct);

    public Task<bool> AnyExistsAsync(CancellationToken ct = default)
        => context.LevelRewards.AnyAsync(ct);

    public async Task AddRangeAsync(List<LevelReward> rewards, CancellationToken ct = default)
    {
        context.LevelRewards.AddRange(rewards);
        await context.SaveChangesAsync(ct);
    }
}

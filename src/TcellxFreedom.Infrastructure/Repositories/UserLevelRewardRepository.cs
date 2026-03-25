using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class UserLevelRewardRepository(ApplicationDbContext context) : IUserLevelRewardRepository
{
    public Task<List<UserLevelReward>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => context.UserLevelRewards
            .Include(r => r.Reward)
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

    public Task<UserLevelReward?> GetByUserAndLevelAsync(string userId, int level, UserTier tier, CancellationToken ct = default)
        => context.UserLevelRewards
            .Include(r => r.Reward)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Level == level && r.Reward.Tier == tier, ct);

    public async Task<UserLevelReward> CreateAsync(UserLevelReward reward, CancellationToken ct = default)
    {
        context.UserLevelRewards.Add(reward);
        await context.SaveChangesAsync(ct);
        return reward;
    }

    public async Task UpdateAsync(UserLevelReward reward, CancellationToken ct = default)
    {
        context.UserLevelRewards.Update(reward);
        await context.SaveChangesAsync(ct);
    }
}

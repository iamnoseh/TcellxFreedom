using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Interfaces;

public interface IUserLevelRewardRepository
{
    Task<List<UserLevelReward>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<UserLevelReward?> GetByUserAndLevelAsync(string userId, int level, UserTier tier, CancellationToken ct = default);
    Task<UserLevelReward> CreateAsync(UserLevelReward reward, CancellationToken ct = default);
    Task UpdateAsync(UserLevelReward reward, CancellationToken ct = default);
}

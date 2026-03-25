using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Interfaces;

public interface ILevelRewardRepository
{
    Task<List<LevelReward>> GetAllAsync(CancellationToken ct = default);
    Task<LevelReward?> GetByLevelAndTierAsync(int level, UserTier tier, CancellationToken ct = default);
    Task<bool> AnyExistsAsync(CancellationToken ct = default);
    Task AddRangeAsync(List<LevelReward> rewards, CancellationToken ct = default);
}

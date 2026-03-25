using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Entities;

public sealed class UserLevelReward
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid LevelRewardId { get; private set; }
    public LevelReward Reward { get; private set; } = null!;
    public int Level { get; private set; }
    public RewardClaimStatus Status { get; private set; }
    public DateTime ClaimedAt { get; private set; }

    private UserLevelReward() { }

    public static UserLevelReward Create(string userId, Guid levelRewardId, int level)
    {
        return new UserLevelReward
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LevelRewardId = levelRewardId,
            Level = level,
            Status = RewardClaimStatus.Pending,
            ClaimedAt = DateTime.UtcNow
        };
    }

    public void MarkClaimed()
    {
        Status = RewardClaimStatus.Claimed;
        ClaimedAt = DateTime.UtcNow;
    }
}

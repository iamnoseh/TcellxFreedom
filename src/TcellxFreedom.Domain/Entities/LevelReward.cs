using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Entities;

public sealed class LevelReward
{
    public Guid Id { get; private set; }
    public int Level { get; private set; }
    public UserTier Tier { get; private set; }
    public RewardType RewardType { get; private set; }
    public string RewardDescription { get; private set; } = string.Empty;
    public int? RewardQuantity { get; private set; }

    private LevelReward() { }

    public static LevelReward Create(
        int level, UserTier tier, RewardType rewardType,
        string rewardDescription, int? rewardQuantity = null)
    {
        return new LevelReward
        {
            Id = Guid.NewGuid(),
            Level = level,
            Tier = tier,
            RewardType = rewardType,
            RewardDescription = rewardDescription,
            RewardQuantity = rewardQuantity
        };
    }
}

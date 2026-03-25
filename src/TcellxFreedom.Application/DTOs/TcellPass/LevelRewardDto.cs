namespace TcellxFreedom.Application.DTOs.TcellPass;

public sealed record LevelRewardDto(
    int Level,
    string Tier,
    string RewardType,
    string RewardDescription,
    int? RewardQuantity,
    bool IsClaimedByCurrentUser
);

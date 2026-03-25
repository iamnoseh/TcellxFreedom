namespace TcellxFreedom.Application.DTOs.TcellPass;

public sealed record CompleteTaskResultDto(
    Guid TaskId,
    int XpAwarded,
    int NewTotalXp,
    int NewLevel,
    bool LeveledUp,
    bool StreakUpdated,
    int CurrentStreakDays,
    bool StreakBonusAwarded,
    int StreakBonusXp
);

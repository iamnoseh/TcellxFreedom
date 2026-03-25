namespace TcellxFreedom.Application.DTOs.TcellPass;

public sealed record UserTcellPassDto(
    string UserId,
    int TotalXp,
    int CurrentLevel,
    int XpToNextLevel,
    int CurrentStreakDays,
    int LongestStreak,
    string Tier,
    DateTime? PremiumExpiresAt,
    List<UserDailyTaskDto> TodayTasks
);

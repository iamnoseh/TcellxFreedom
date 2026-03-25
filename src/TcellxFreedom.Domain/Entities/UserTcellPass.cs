using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Entities;

public sealed class UserTcellPass
{
    private static readonly int[] LevelXpThresholds =
        [0, 100, 200, 350, 500, 700, 950, 1250, 1600, 2000, 2500, 3100, 3800, 4600, 5500, 6500, 7600, 8800, 10100, 11500, 13000];

    private readonly List<UserDailyTask> _dailyTasks = [];

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public int TotalXp { get; private set; }
    public int CurrentLevel { get; private set; }
    public int CurrentStreakDays { get; private set; }
    public int LongestStreak { get; private set; }
    public UserTier Tier { get; private set; }
    public DateTime? PremiumExpiresAt { get; private set; }
    public DateTime? LastStreakDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<UserDailyTask> DailyTasks => _dailyTasks.AsReadOnly();

    private UserTcellPass() { }

    public static UserTcellPass Create(string userId)
    {
        return new UserTcellPass
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalXp = 0,
            CurrentLevel = 1,
            CurrentStreakDays = 0,
            LongestStreak = 0,
            Tier = UserTier.Free,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddXp(int xp)
    {
        TotalXp += xp;
        RecalculateLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordStreakCompletion(DateTime utcDate)
    {
        var completionDate = utcDate.Date;
        if (LastStreakDate.HasValue && LastStreakDate.Value.Date == completionDate.AddDays(-1))
            CurrentStreakDays++;
        else
            CurrentStreakDays = 1;

        if (CurrentStreakDays > LongestStreak)
            LongestStreak = CurrentStreakDays;

        LastStreakDate = completionDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetStreak()
    {
        CurrentStreakDays = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ActivatePremium(DateTime expiresAt)
    {
        Tier = UserTier.Premium;
        PremiumExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeactivatePremium()
    {
        Tier = UserTier.Free;
        PremiumExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CheckPremiumExpiry()
    {
        if (Tier == UserTier.Premium && PremiumExpiresAt.HasValue && PremiumExpiresAt.Value <= DateTime.UtcNow)
        {
            DeactivatePremium();
            return true;
        }
        return false;
    }

    public static int GetXpForLevel(int level)
    {
        if (level < 1 || level > 20) return 0;
        return LevelXpThresholds[level];
    }

    public int XpToNextLevel()
    {
        if (CurrentLevel >= 20) return 0;
        return LevelXpThresholds[CurrentLevel] - TotalXp;
    }

    private void RecalculateLevel()
    {
        int newLevel = 1;
        for (int i = 1; i < LevelXpThresholds.Length; i++)
        {
            if (TotalXp >= LevelXpThresholds[i])
                newLevel = i;
            else
                break;
        }
        CurrentLevel = Math.Min(newLevel, 20);
    }
}

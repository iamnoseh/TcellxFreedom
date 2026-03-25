namespace TcellxFreedom.Application.DTOs.TcellPass;

public sealed record LeaderboardEntryDto(
    int Rank,
    string UserId,
    string DisplayName,
    int TotalXp,
    int CurrentLevel,
    string Tier
);

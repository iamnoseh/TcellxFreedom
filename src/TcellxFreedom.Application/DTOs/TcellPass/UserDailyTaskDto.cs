namespace TcellxFreedom.Application.DTOs.TcellPass;

public sealed record UserDailyTaskDto(
    Guid Id,
    string Title,
    string Description,
    int XpReward,
    string Category,
    bool IsPremiumOnly,
    string Status,
    DateTime? CompletedAt
);

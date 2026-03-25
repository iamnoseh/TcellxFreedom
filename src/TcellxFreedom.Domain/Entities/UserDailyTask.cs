using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Entities;

public sealed class UserDailyTask
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid UserTcellPassId { get; private set; }
    public Guid PassTaskTemplateId { get; private set; }
    public PassTaskTemplate Template { get; private set; } = null!;
    public int AssignedDayNumber { get; private set; }
    public DateOnly AssignedDate { get; private set; }
    public DailyTaskStatus Status { get; private set; }
    public int XpAwarded { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserDailyTask() { }

    public static UserDailyTask Create(
        string userId, Guid userTcellPassId, Guid templateId,
        int dayNumber, DateOnly assignedDate)
    {
        return new UserDailyTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserTcellPassId = userTcellPassId,
            PassTaskTemplateId = templateId,
            AssignedDayNumber = dayNumber,
            AssignedDate = assignedDate,
            Status = DailyTaskStatus.Pending,
            XpAwarded = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete(int xpAwarded)
    {
        Status = DailyTaskStatus.Completed;
        XpAwarded = xpAwarded;
        CompletedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = DailyTaskStatus.Expired;
    }
}

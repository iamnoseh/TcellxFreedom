using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Entities;

public sealed class TaskNotification
{
    public Guid Id { get; private set; }
    public Guid PlanTaskId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime ScheduledAt { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string NotificationTitle { get; private set; } = string.Empty;
    public string NotificationBody { get; private set; } = string.Empty;
    public string? HangfireJobId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private TaskNotification() { }

    public static TaskNotification Create(
        Guid planTaskId,
        string userId,
        DateTime scheduledAt,
        string title,
        string body)
    {
        return new TaskNotification
        {
            Id = Guid.NewGuid(),
            PlanTaskId = planTaskId,
            UserId = userId,
            ScheduledAt = scheduledAt,
            Status = NotificationStatus.Pending,
            NotificationTitle = title,
            NotificationBody = body,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetHangfireJobId(string jobId)
    {
        HangfireJobId = jobId;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = NotificationStatus.Failed;
    }

    public void Cancel()
    {
        Status = NotificationStatus.Cancelled;
    }
}

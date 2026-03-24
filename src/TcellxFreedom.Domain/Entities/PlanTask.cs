using TcellxFreedom.Domain.Enums;
using TaskStatus = TcellxFreedom.Domain.Enums.TaskStatus;

namespace TcellxFreedom.Domain.Entities;

public sealed class PlanTask
{
    public Guid Id { get; private set; }
    public Guid PlanId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public TaskStatus Status { get; private set; }
    public bool IsAiSuggested { get; private set; }
    public bool IsAccepted { get; private set; }
    public RecurrenceType Recurrence { get; private set; }
    public int? RecurrenceIntervalDays { get; private set; }
    public Guid? ParentTaskId { get; private set; }
    public string? AiRationale { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private PlanTask() { }

    public static PlanTask Create(
        Guid planId,
        string title,
        string? description,
        DateTime scheduledAt,
        int estimatedMinutes,
        bool isAiSuggested,
        RecurrenceType recurrence = RecurrenceType.None,
        Guid? parentTaskId = null)
    {
        return new PlanTask
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Title = title,
            Description = description,
            ScheduledAt = scheduledAt,
            EstimatedMinutes = estimatedMinutes,
            Status = TaskStatus.Pending,
            IsAiSuggested = isAiSuggested,
            IsAccepted = !isAiSuggested,
            Recurrence = recurrence,
            RecurrenceIntervalDays = recurrence == RecurrenceType.Weekly ? 7 : recurrence == RecurrenceType.Daily ? 1 : null,
            ParentTaskId = parentTaskId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        IsAccepted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        IsAccepted = false;
        Status = TaskStatus.Skipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkComplete()
    {
        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkInProgress()
    {
        Status = TaskStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newScheduledAt)
    {
        ScheduledAt = newScheduledAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string? title, string? description, DateTime? scheduledAt, int? estimatedMinutes)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (scheduledAt is not null) ScheduledAt = scheduledAt.Value;
        if (estimatedMinutes is not null) EstimatedMinutes = estimatedMinutes.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAiRationale(string rationale)
    {
        AiRationale = rationale;
    }

    public void SetRecurrence(RecurrenceType type, int intervalDays)
    {
        Recurrence = type;
        RecurrenceIntervalDays = intervalDays;
        UpdatedAt = DateTime.UtcNow;
    }
}

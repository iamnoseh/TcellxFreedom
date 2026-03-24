using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Entities;

public sealed class Plan
{
    private readonly List<PlanTask> _tasks = [];

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public PlanStatus Status { get; private set; }
    public string? AiContext { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<PlanTask> Tasks => _tasks.AsReadOnly();

    private Plan() { }

    public static Plan Create(string userId, string title, string? description, DateTime startDate, DateTime endDate)
    {
        return new Plan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            Status = PlanStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        Status = PlanStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = PlanStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = PlanStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAiContext(string context)
    {
        AiContext = context;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTask(PlanTask task)
    {
        _tasks.Add(task);
    }
}

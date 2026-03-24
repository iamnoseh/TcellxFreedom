namespace TcellxFreedom.Domain.Entities;

public sealed class UserTaskStatistic
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime WeekStartDate { get; private set; }
    public int TotalTasks { get; private set; }
    public int CompletedTasks { get; private set; }
    public int SkippedTasks { get; private set; }
    public decimal CompletionRate { get; private set; }
    public string? AiImprovementSuggestions { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private UserTaskStatistic() { }

    public static UserTaskStatistic Create(string userId, DateTime weekStart, int total, int completed, int skipped)
    {
        var rate = total > 0 ? Math.Round((decimal)completed / total * 100, 2) : 0m;
        return new UserTaskStatistic
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WeekStartDate = weekStart,
            TotalTasks = total,
            CompletedTasks = completed,
            SkippedTasks = skipped,
            CompletionRate = rate,
            CalculatedAt = DateTime.UtcNow
        };
    }

    public void Recalculate(int total, int completed, int skipped)
    {
        TotalTasks = total;
        CompletedTasks = completed;
        SkippedTasks = skipped;
        CompletionRate = total > 0 ? Math.Round((decimal)completed / total * 100, 2) : 0m;
        CalculatedAt = DateTime.UtcNow;
    }

    public void SetAiSuggestions(string suggestionsJson)
    {
        AiImprovementSuggestions = suggestionsJson;
    }
}

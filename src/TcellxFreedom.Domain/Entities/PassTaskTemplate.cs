using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Domain.Entities;

public sealed class PassTaskTemplate
{
    public Guid Id { get; private set; }
    public int DayNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int XpReward { get; private set; }
    public TaskCategory Category { get; private set; }
    public bool IsPremiumOnly { get; private set; }
    public int SortOrder { get; private set; }

    private PassTaskTemplate() { }

    public static PassTaskTemplate Create(
        int dayNumber, string title, string description,
        int xpReward, TaskCategory category, bool isPremiumOnly, int sortOrder)
    {
        return new PassTaskTemplate
        {
            Id = Guid.NewGuid(),
            DayNumber = dayNumber,
            Title = title,
            Description = description,
            XpReward = xpReward,
            Category = category,
            IsPremiumOnly = isPremiumOnly,
            SortOrder = sortOrder
        };
    }
}

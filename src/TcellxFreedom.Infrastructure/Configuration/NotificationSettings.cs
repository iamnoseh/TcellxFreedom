namespace TcellxFreedom.Infrastructure.Configuration;

public sealed class NotificationSettings
{
    public const string SectionName = "Notifications";
    public int MinOffsetMinutes { get; init; } = 10;
    public int MaxOffsetMinutes { get; init; } = 12;
}

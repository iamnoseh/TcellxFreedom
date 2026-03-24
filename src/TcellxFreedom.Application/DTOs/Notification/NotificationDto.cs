namespace TcellxFreedom.Application.DTOs.Notification;

public sealed record NotificationDto(
    Guid Id,
    Guid PlanTaskId,
    string TaskTitle,
    DateTime ScheduledAt,
    string Status,
    string NotificationTitle,
    string NotificationBody
);

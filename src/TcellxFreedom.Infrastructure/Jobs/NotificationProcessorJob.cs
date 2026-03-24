using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Infrastructure.Jobs;

public sealed class NotificationProcessorJob(ITaskNotificationRepository notificationRepository)
    : INotificationProcessor
{
    public async Task ProcessAsync(Guid notificationId)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId);
        if (notification is null) return;
        if (notification.Status != NotificationStatus.Pending) return;

        notification.MarkSent();
        await notificationRepository.UpdateAsync(notification);
    }
}

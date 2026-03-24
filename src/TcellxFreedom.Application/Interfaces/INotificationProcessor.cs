namespace TcellxFreedom.Application.Interfaces;

public interface INotificationProcessor
{
    Task ProcessAsync(Guid notificationId);
}

using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Notification;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetUpcomingNotifications;

public sealed class GetUpcomingNotificationsQueryHandler(
    ITaskNotificationRepository notificationRepository,
    IPlanTaskRepository taskRepository)
    : IRequestHandler<GetUpcomingNotificationsQuery, Response<List<NotificationDto>>>
{
    public async Task<Response<List<NotificationDto>>> Handle(GetUpcomingNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await notificationRepository.GetUpcomingByUserIdAsync(
            request.UserId, request.From, request.To, cancellationToken);

        var taskIds = notifications.Select(n => n.PlanTaskId).Distinct().ToList();
        var tasks = new Dictionary<Guid, string>();
        foreach (var taskId in taskIds)
        {
            var task = await taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task is not null)
                tasks[taskId] = task.Title;
        }

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id,
            n.PlanTaskId,
            tasks.TryGetValue(n.PlanTaskId, out var title) ? title : string.Empty,
            n.ScheduledAt,
            n.Status.ToString(),
            n.NotificationTitle,
            n.NotificationBody
        )).ToList();

        return new Response<List<NotificationDto>>(dtos);
    }
}

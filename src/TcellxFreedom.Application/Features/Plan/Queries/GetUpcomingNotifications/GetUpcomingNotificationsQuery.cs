using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Notification;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetUpcomingNotifications;

public sealed record GetUpcomingNotificationsQuery(
    string UserId,
    DateTime From,
    DateTime To
) : IRequest<Response<List<NotificationDto>>>;

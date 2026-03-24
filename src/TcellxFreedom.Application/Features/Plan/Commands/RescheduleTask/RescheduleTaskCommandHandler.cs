using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Commands.RescheduleTask;

public sealed class RescheduleTaskCommandHandler(
    IPlanTaskRepository taskRepository,
    IPlanRepository planRepository,
    INotificationService notificationService)
    : IRequestHandler<RescheduleTaskCommand, Response<PlanTaskDto>>
{
    public async Task<Response<PlanTaskDto>> Handle(RescheduleTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return new Response<PlanTaskDto>(HttpStatusCode.NotFound, "Вазифа ёфт нашуд.");

        var plan = await planRepository.GetByIdAsync(task.PlanId, cancellationToken);
        if (plan is null || plan.UserId != request.UserId)
            return new Response<PlanTaskDto>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст.");

        await notificationService.CancelForTaskAsync(task.Id, cancellationToken);

        task.Reschedule(request.NewScheduledAt);
        await taskRepository.UpdateAsync(task, cancellationToken);

        await notificationService.ScheduleForTaskAsync(task, plan.UserId, cancellationToken);

        return new Response<PlanTaskDto>(task.ToDto());
    }
}

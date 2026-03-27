using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Commands.CompleteTask;

public sealed class CompleteTaskCommandHandler(
    IPlanTaskRepository taskRepository,
    IPlanRepository planRepository,
    INotificationService notificationService)
    : IRequestHandler<CompleteTaskCommand, Response<PlanTaskDto>>
{
    public async Task<Response<PlanTaskDto>> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return new Response<PlanTaskDto>(HttpStatusCode.NotFound, "Задача не найдена.");

        var plan = await planRepository.GetByIdAsync(task.PlanId, cancellationToken);
        if (plan is null || plan.UserId != request.UserId)
            return new Response<PlanTaskDto>(HttpStatusCode.Forbidden, "Доступ запрещён.");

        task.MarkComplete();
        await taskRepository.UpdateAsync(task, cancellationToken);
        await notificationService.CancelForTaskAsync(task.Id, cancellationToken);

        return new Response<PlanTaskDto>(task.ToDto());
    }
}

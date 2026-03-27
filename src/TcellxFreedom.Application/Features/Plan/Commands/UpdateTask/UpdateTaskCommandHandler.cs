using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.Common.Mappers;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler(
    IPlanTaskRepository taskRepository,
    IPlanRepository planRepository)
    : IRequestHandler<UpdateTaskCommand, Response<PlanTaskDto>>
{
    public async Task<Response<PlanTaskDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return new Response<PlanTaskDto>(HttpStatusCode.NotFound, "Задача не найдена.");

        var plan = await planRepository.GetByIdAsync(task.PlanId, cancellationToken);
        if (plan is null || plan.UserId != request.UserId)
            return new Response<PlanTaskDto>(HttpStatusCode.Forbidden, "Доступ запрещён.");

        if (plan.Status != PlanStatus.Draft)
            return new Response<PlanTaskDto>(HttpStatusCode.BadRequest, "Изменять можно только задачи планов в статусе черновика.");

        task.UpdateDetails(request.Title, request.Description, request.ScheduledAt, request.EstimatedMinutes);
        await taskRepository.UpdateAsync(task, cancellationToken);

        return new Response<PlanTaskDto>(task.ToDto());
    }
}

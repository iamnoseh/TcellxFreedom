using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TaskStatus = TcellxFreedom.Domain.Enums.TaskStatus;

namespace TcellxFreedom.Application.Common.Mappers;

public static class PlanMapper
{
    public static PlanTaskDto ToDto(this PlanTask task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.ScheduledAt,
        task.EstimatedMinutes,
        task.Status.ToString(),
        task.IsAiSuggested,
        task.IsAccepted,
        task.Recurrence.ToString(),
        task.AiRationale);

    public static PlanDetailDto ToDetailDto(this Plan plan) => new(
        plan.Id,
        plan.Title,
        plan.Description,
        plan.StartDate,
        plan.EndDate,
        plan.Status.ToString(),
        plan.Tasks.Select(t => t.ToDto()).ToList(),
        plan.CreatedAt);

    public static PlanDto ToPlanDto(this Plan plan) => new(
        plan.Id,
        plan.Title,
        plan.Description,
        plan.StartDate,
        plan.EndDate,
        plan.Status.ToString(),
        plan.Tasks.Count,
        plan.Tasks.Count(t => t.Status == TaskStatus.Completed),
        plan.CreatedAt);
}

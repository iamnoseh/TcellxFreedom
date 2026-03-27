using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.CompleteTask;

public sealed class CompletePassTaskCommandHandler(
    IUserDailyTaskRepository dailyTaskRepository,
    IUserTcellPassRepository passRepository)
    : IRequestHandler<CompletePassTaskCommand, Response<CompleteTaskResultDto>>
{
    private const int StreakBonusXp = 30;

    public async Task<Response<CompleteTaskResultDto>> Handle(CompletePassTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dailyTaskRepository.GetByIdWithTemplateAsync(request.TaskId, cancellationToken);
        if (task is null)
            return new Response<CompleteTaskResultDto>(HttpStatusCode.NotFound, "Задача не найдена.");

        if (task.UserId != request.UserId)
            return new Response<CompleteTaskResultDto>(HttpStatusCode.Forbidden, "Доступ запрещён.");

        if (task.Status != DailyTaskStatus.Pending)
            return new Response<CompleteTaskResultDto>(HttpStatusCode.BadRequest, "Эта задача не находится в состоянии ожидания.");

        var pass = await passRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (pass is null)
        {
            pass = UserTcellPass.Create(request.UserId);
            await passRepository.CreateAsync(pass, cancellationToken);
        }
        else
        {
            pass.CheckPremiumExpiry();
        }

        int baseXp = task.Template.XpReward;
        int xpAwarded = pass.Tier == UserTier.Premium ? baseXp * 2 : baseXp;

        task.Complete(xpAwarded);
        await dailyTaskRepository.UpdateAsync(task, cancellationToken);

        int oldLevel = pass.CurrentLevel;
        pass.AddXp(xpAwarded);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Количество всех назначенных задач на сегодня (для бесплатных=3, для премиум=5)
        int totalTasksToday = await dailyTaskRepository.CountByUserAndDateAsync(request.UserId, today, cancellationToken);
        int completedCount = await dailyTaskRepository.CountCompletedByUserAndDateAsync(request.UserId, today, cancellationToken);

        bool streakBonusAwarded = false;
        bool streakUpdated = false;

        // Бонус серии даём, когда пользователь выполнил ВСЕ задачи дня
        if (totalTasksToday > 0 && completedCount >= totalTasksToday)
        {
            pass.AddXp(StreakBonusXp);
            pass.RecordStreakCompletion(DateTime.UtcNow);
            streakBonusAwarded = true;
            streakUpdated = true;
        }

        await passRepository.UpdateAsync(pass, cancellationToken);

        return new Response<CompleteTaskResultDto>(new CompleteTaskResultDto(
            TaskId: task.Id,
            XpAwarded: xpAwarded,
            NewTotalXp: pass.TotalXp,
            NewLevel: pass.CurrentLevel,
            LeveledUp: pass.CurrentLevel > oldLevel,
            StreakUpdated: streakUpdated,
            CurrentStreakDays: pass.CurrentStreakDays,
            StreakBonusAwarded: streakBonusAwarded,
            StreakBonusXp: streakBonusAwarded ? StreakBonusXp : 0
        ));
    }
}

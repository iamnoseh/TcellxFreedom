using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetMyPass;

public sealed class GetMyPassQueryHandler(
    IUserTcellPassRepository passRepository,
    IUserDailyTaskRepository dailyTaskRepository,
    IPassTaskTemplateRepository templateRepository)
    : IRequestHandler<GetMyPassQuery, Response<UserTcellPassDto>>
{
    public async Task<Response<UserTcellPassDto>> Handle(GetMyPassQuery request, CancellationToken cancellationToken)
    {
        var pass = await passRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (pass is null)
        {
            pass = UserTcellPass.Create(request.UserId);
            await passRepository.CreateAsync(pass, cancellationToken);
        }
        else if (pass.CheckPremiumExpiry())
        {
            await passRepository.UpdateAsync(pass, cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existingCount = await dailyTaskRepository.CountByUserAndDateAsync(request.UserId, today, cancellationToken);
        if (existingCount == 0)
        {
            var daysSinceStart = (int)(today.ToDateTime(TimeOnly.MinValue) - pass.CreatedAt.Date).TotalDays;
            var dayNumber = (daysSinceStart % 20) + 1;
            var templates = await templateRepository.GetByDayNumberAsync(dayNumber, cancellationToken);

            IEnumerable<PassTaskTemplate> filtered = pass.Tier == UserTier.Premium
                ? templates.Take(5)
                : templates.Where(t => !t.IsPremiumOnly).Take(3);

            var newTasks = filtered
                .Select(t => UserDailyTask.Create(pass.UserId, pass.Id, t.Id, dayNumber, today))
                .ToList();

            if (newTasks.Count > 0)
                await dailyTaskRepository.AddRangeAsync(newTasks, cancellationToken);
        }

        var todayTasks = await dailyTaskRepository.GetByUserAndDateAsync(request.UserId, today, cancellationToken);

        var taskDtos = todayTasks.Select(t => new UserDailyTaskDto(
            t.Id,
            t.Template.Title,
            t.Template.Description,
            t.Template.XpReward,
            t.Template.Category.ToString(),
            t.Template.IsPremiumOnly,
            t.Status.ToString(),
            t.CompletedAt
        )).ToList();

        var dto = new UserTcellPassDto(
            pass.UserId,
            pass.TotalXp,
            pass.CurrentLevel,
            pass.XpToNextLevel(),
            pass.CurrentStreakDays,
            pass.LongestStreak,
            pass.Tier.ToString(),
            pass.PremiumExpiresAt,
            taskDtos
        );

        return new Response<UserTcellPassDto>(dto);
    }
}

using Microsoft.Extensions.Logging;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Infrastructure.Jobs;

public sealed class DailyTaskAssignmentJob(
    IUserTcellPassRepository passRepository,
    IPassTaskTemplateRepository templateRepository,
    IUserDailyTaskRepository dailyTaskRepository,
    ILogger<DailyTaskAssignmentJob> logger)
{
    public async Task ExecuteAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var passes = await passRepository.GetAllActiveAsync();

        foreach (var pass in passes)
        {
            try
            {
                await AssignTasksForUserAsync(pass, today);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при назначении ежедневных задач для пользователя {UserId}", pass.UserId);
            }
        }

        logger.LogInformation("DailyTaskAssignmentJob: задачи за {Date} назначены. Пользователей: {Count}.", today, passes.Count);
    }

    private async Task AssignTasksForUserAsync(UserTcellPass pass, DateOnly today)
    {
        var existingCount = await dailyTaskRepository.CountByUserAndDateAsync(pass.UserId, today);
        if (existingCount > 0) return;

        var daysSinceStart = (int)(today.ToDateTime(TimeOnly.MinValue) - pass.CreatedAt.Date).TotalDays;
        var dayNumber = (daysSinceStart % 20) + 1;

        var templates = await templateRepository.GetByDayNumberAsync(dayNumber);

        IEnumerable<PassTaskTemplate> filtered = pass.Tier == UserTier.Premium
            ? templates.Take(5)
            : templates.Where(t => !t.IsPremiumOnly).Take(3);

        var tasks = filtered.Select(t =>
            UserDailyTask.Create(pass.UserId, pass.Id, t.Id, dayNumber, today))
            .ToList();

        if (tasks.Count > 0)
            await dailyTaskRepository.AddRangeAsync(tasks);
    }
}

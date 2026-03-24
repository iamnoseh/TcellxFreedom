using System.Text.Json;
using Microsoft.Extensions.Logging;
using TcellxFreedom.Application.DTOs.Gemini;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Interfaces;
using TaskStatus = TcellxFreedom.Domain.Enums.TaskStatus;

namespace TcellxFreedom.Infrastructure.Jobs;

public sealed class WeeklyStatisticsCalculatorJob(
    IPlanTaskRepository taskRepository,
    IPlanRepository planRepository,
    IUserTaskStatisticRepository statisticsRepository,
    IGeminiService geminiService,
    ILogger<WeeklyStatisticsCalculatorJob> logger)
{
    public async Task ExecuteAsync()
    {
        var (weekStart, weekEnd) = GetCurrentWeekRange();
        var tasks = await taskRepository.GetAllByDateRangeAsync(weekStart, weekEnd);
        if (tasks.Count == 0) return;

        var planUserMap = await BuildPlanUserMapAsync(tasks.Select(t => t.PlanId).Distinct());
        var userGroups = tasks
            .Where(t => planUserMap.ContainsKey(t.PlanId))
            .GroupBy(t => planUserMap[t.PlanId]);

        foreach (var group in userGroups)
            await ProcessUserWeekAsync(group.Key, group.ToList(), weekStart);
    }

    private async Task ProcessUserWeekAsync(string userId, List<PlanTask> tasks, DateTime weekStart)
    {
        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == TaskStatus.Completed);
        var skipped = tasks.Count(t => t.Status == TaskStatus.Skipped);

        var stat = await statisticsRepository.GetByUserAndWeekAsync(userId, weekStart);
        if (stat is not null)
        {
            stat.Recalculate(total, completed, skipped);
            await statisticsRepository.UpdateAsync(stat);
        }
        else
        {
            stat = UserTaskStatistic.Create(userId, weekStart, total, completed, skipped);
            await statisticsRepository.CreateAsync(stat);
        }

        if (stat.CompletionRate < 70m)
            await TryGenerateAiSuggestionsAsync(userId, stat);
    }

    private async Task TryGenerateAiSuggestionsAsync(string userId, UserTaskStatistic stat)
    {
        var history = await statisticsRepository.GetByUserIdAsync(userId, 4);
        if (history.Count < 2) return;

        try
        {
            var statsRequest = new GeminiStatsRequest(
                userId,
                history.Select(h => new WeeklyStatSummary(h.WeekStartDate, h.CompletionRate, h.TotalTasks, h.CompletedTasks)).ToList());

            var suggestions = await geminiService.GenerateImprovementSuggestionsAsync(statsRequest);
            stat.SetAiSuggestions(JsonSerializer.Serialize(suggestions));
            await statisticsRepository.UpdateAsync(stat);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Хатогӣ ҳангоми тавлиди тавсияҳои ИИ барои корбар {UserId}", userId);
        }
    }

    private async Task<Dictionary<Guid, string>> BuildPlanUserMapAsync(IEnumerable<Guid> planIds)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var planId in planIds)
        {
            var plan = await planRepository.GetByIdAsync(planId);
            if (plan is not null) map[planId] = plan.UserId;
        }
        return map;
    }

    private static (DateTime weekStart, DateTime weekEnd) GetCurrentWeekRange()
    {
        var today = DateTime.UtcNow.Date;
        var weekEnd = today.AddDays(-(int)today.DayOfWeek);
        return (weekEnd.AddDays(-6), weekEnd);
    }
}

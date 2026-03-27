using Microsoft.Extensions.Logging;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Infrastructure.Jobs;

public sealed class ExpireOldTasksJob(
    IUserDailyTaskRepository dailyTaskRepository,
    ILogger<ExpireOldTasksJob> logger)
{
    public async Task ExecuteAsync()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var pendingTasks = await dailyTaskRepository.GetPendingByDateAsync(yesterday);

        foreach (var task in pendingTasks)
            task.Expire();

        if (pendingTasks.Count > 0)
            await dailyTaskRepository.UpdateRangeAsync(pendingTasks);

        logger.LogInformation("ExpireOldTasksJob: {Count} задач за {Date} истекли.", pendingTasks.Count, yesterday);
    }
}

using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Domain.Interfaces;

public interface IUserTaskStatisticRepository
{
    Task<List<UserTaskStatistic>> GetByUserIdAsync(string userId, int weekCount, CancellationToken ct = default);
    Task<UserTaskStatistic?> GetByUserAndWeekAsync(string userId, DateTime weekStart, CancellationToken ct = default);
    Task CreateAsync(UserTaskStatistic stat, CancellationToken ct = default);
    Task UpdateAsync(UserTaskStatistic stat, CancellationToken ct = default);
}

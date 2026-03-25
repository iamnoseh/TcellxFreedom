using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Domain.Interfaces;

public interface IUserDailyTaskRepository
{
    Task<List<UserDailyTask>> GetByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default);
    Task<UserDailyTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDailyTask?> GetByIdWithTemplateAsync(Guid id, CancellationToken ct = default);
    Task AddRangeAsync(List<UserDailyTask> tasks, CancellationToken ct = default);
    Task UpdateAsync(UserDailyTask task, CancellationToken ct = default);
    Task UpdateRangeAsync(List<UserDailyTask> tasks, CancellationToken ct = default);
    Task<List<UserDailyTask>> GetPendingByDateAsync(DateOnly date, CancellationToken ct = default);
    Task<int> CountCompletedByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default);
    Task<int> CountByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default);
}

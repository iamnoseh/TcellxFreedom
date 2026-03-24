using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Domain.Interfaces;

public interface IPlanTaskRepository
{
    Task<PlanTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PlanTask>> GetByPlanIdAsync(Guid planId, CancellationToken ct = default);
    Task<List<PlanTask>> GetByUserAndDateRangeAsync(string userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<PlanTask>> GetAllByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<PlanTask>> GetPendingRecurringTasksAsync(CancellationToken ct = default);
    Task UpdateAsync(PlanTask task, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PlanTask> tasks, CancellationToken ct = default);
}

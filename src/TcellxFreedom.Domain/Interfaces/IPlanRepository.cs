using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Domain.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Plan?> GetByIdWithTasksAsync(Guid id, CancellationToken ct = default);
    Task<List<Plan>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Plan> CreateAsync(Plan plan, CancellationToken ct = default);
    Task UpdateAsync(Plan plan, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

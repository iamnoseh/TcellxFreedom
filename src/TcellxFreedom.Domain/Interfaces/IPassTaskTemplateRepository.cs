using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Domain.Interfaces;

public interface IPassTaskTemplateRepository
{
    Task<List<PassTaskTemplate>> GetByDayNumberAsync(int dayNumber, CancellationToken ct = default);
    Task<List<PassTaskTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<PassTaskTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> AnyExistsAsync(CancellationToken ct = default);
    Task AddRangeAsync(List<PassTaskTemplate> templates, CancellationToken ct = default);
}

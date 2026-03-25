using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class PassTaskTemplateRepository(ApplicationDbContext context) : IPassTaskTemplateRepository
{
    public Task<List<PassTaskTemplate>> GetByDayNumberAsync(int dayNumber, CancellationToken ct = default)
        => context.PassTaskTemplates
            .Where(t => t.DayNumber == dayNumber)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);

    public Task<List<PassTaskTemplate>> GetAllAsync(CancellationToken ct = default)
        => context.PassTaskTemplates.OrderBy(t => t.DayNumber).ThenBy(t => t.SortOrder).ToListAsync(ct);

    public Task<PassTaskTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.PassTaskTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> AnyExistsAsync(CancellationToken ct = default)
        => context.PassTaskTemplates.AnyAsync(ct);

    public async Task AddRangeAsync(List<PassTaskTemplate> templates, CancellationToken ct = default)
    {
        context.PassTaskTemplates.AddRange(templates);
        await context.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;
using TcellxFreedom.Infrastructure.Mappers;

namespace TcellxFreedom.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var appUser = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);

        return appUser?.ToDomain();
    }

    public async Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var appUser = await _context.Users
            .FindAsync(new object[] { userId }, cancellationToken);

        return appUser?.ToDomain();
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var appUser = user.ToInfrastructure();
        await _context.Users.AddAsync(appUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Users.FindAsync(new object[] { user.Id }, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"User with ID {user.Id} not found");

        var updated = user.ToInfrastructure(existing);
        _context.Users.Update(updated);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetDisplayNamesByIdsAsync(
        IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        return await _context.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => $"{u.FirstName} {u.LastName}".Trim(),
                cancellationToken);
    }
}

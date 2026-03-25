using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Domain.Interfaces;

public interface IUserTcellPassRepository
{
    Task<UserTcellPass?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<List<UserTcellPass>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<UserTcellPass>> GetTopByXpAsync(int topN = 50, CancellationToken ct = default);
    Task<UserTcellPass> CreateAsync(UserTcellPass pass, CancellationToken ct = default);
    Task UpdateAsync(UserTcellPass pass, CancellationToken ct = default);
}

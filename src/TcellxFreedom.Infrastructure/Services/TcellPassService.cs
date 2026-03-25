using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class TcellPassService(IUserRepository userRepository) : ITcellPassService
{
    public async Task<bool> ProcessPremiumPaymentAsync(string userId, decimal amount, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return false;

        try
        {
            user.DeductBalance(amount);
            await userRepository.UpdateAsync(user, ct);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

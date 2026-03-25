namespace TcellxFreedom.Application.Interfaces;

public interface ITcellPassService
{
    Task<bool> ProcessPremiumPaymentAsync(string userId, decimal amount, CancellationToken ct = default);
}

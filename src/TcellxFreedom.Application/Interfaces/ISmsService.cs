namespace TcellxFreedom.Application.Interfaces;

public interface ISmsService
{
    Task<string> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default);
}

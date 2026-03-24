namespace TcellxFreedom.Application.Interfaces;

public interface IOtpVerifier
{
    Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default);
}

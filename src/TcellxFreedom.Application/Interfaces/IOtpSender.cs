namespace TcellxFreedom.Application.Interfaces;

public interface IOtpSender
{
    Task<string> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
}

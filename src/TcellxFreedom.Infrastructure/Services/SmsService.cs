using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class SmsService : ISmsService
{
    private readonly IOsonSmsService _osonSmsService;
    private readonly Dictionary<string, string> _otpStorage = new();

    public SmsService(IOsonSmsService osonSmsService)
    {
        _osonSmsService = osonSmsService;
    }

    public async Task<string> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var otpCode = GenerateOtpCode();
        var message = $"Кодиi тасдиқи шумо: {otpCode}";

        var result = await _osonSmsService.SendSmsAsync(phoneNumber, message);

        if (result.IsSuccess)
        {
            _otpStorage[phoneNumber] = otpCode;
            return otpCode;
        }

        throw new InvalidOperationException(result.Message ?? "Хатогӣ дар равонкунии SMS");
    }

    public Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        if (!_otpStorage.TryGetValue(phoneNumber, out var storedOtp))
            return Task.FromResult(false);

        var isValid = storedOtp == otpCode;

        if (isValid)
            _otpStorage.Remove(phoneNumber);

        return Task.FromResult(isValid);
    }

    private static string GenerateOtpCode()
    {
        return Random.Shared.Next(1000, 9999).ToString();
    }
}

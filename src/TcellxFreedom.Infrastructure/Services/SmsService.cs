using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class SmsService : ISmsService
{
    private readonly IOsonSmsService _osonSmsService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SmsService> _logger;
    private const int OtpExpirationMinutes = 5;

    public SmsService(IOsonSmsService osonSmsService, IMemoryCache cache, ILogger<SmsService> logger)
    {
        _osonSmsService = osonSmsService;
        _cache = cache;
        _logger = logger;
    }


    public async Task<string> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        var otpCode = GenerateOtpCode();
        var message = $"Ваш код подтверждения: {otpCode}";

        var result = await _osonSmsService.SendSmsAsync(phoneNumber, message);

        var cacheKey = GetOtpCacheKey(normalizedPhone);
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpirationMinutes)
        };
        _cache.Set(cacheKey, otpCode, cacheOptions);

        // DEV: log OTP to console so you can test without real SMS
        _logger.LogWarning(">>> OTP for {Phone}: {Code} <<<", normalizedPhone, otpCode);

        if (!result.IsSuccess)
            _logger.LogError("SMS send failed: {Message}. Using cached OTP for dev.", result.Message);

        return otpCode;
    }

    public Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        var cacheKey = GetOtpCacheKey(normalizedPhone);

        if (!_cache.TryGetValue(cacheKey, out string? storedOtp))
            return Task.FromResult(false);

        var isValid = storedOtp == otpCode;

        if (isValid)
            _cache.Remove(cacheKey);

        return Task.FromResult(isValid);
    }

    private static string GenerateOtpCode()
    {
        return Random.Shared.Next(1000, 9999).ToString();
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return digitsOnly;
    }

    private static string GetOtpCacheKey(string phoneNumber)
    {
        return $"otp_{phoneNumber}";
    }
}

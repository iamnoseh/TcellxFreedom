using Microsoft.Extensions.Caching.Memory;
using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class SmsService : ISmsService
{
    private readonly IOsonSmsService _osonSmsService;
    private readonly IMemoryCache _cache;
    private const int OtpExpirationMinutes = 5;

    public SmsService(IOsonSmsService osonSmsService, IMemoryCache cache)
    {
        _osonSmsService = osonSmsService;
        _cache = cache;
    }

    public async Task<string> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        var otpCode = GenerateOtpCode();
        var message = $"Ваш код подтверждения: {otpCode}";

        var result = await _osonSmsService.SendSmsAsync(phoneNumber, message);

        if (result.IsSuccess)
        {
            var cacheKey = GetOtpCacheKey(normalizedPhone);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpirationMinutes)
            };

            _cache.Set(cacheKey, otpCode, cacheOptions);
            return otpCode;
        }

        throw new InvalidOperationException(result.Message ?? "Ошибка при отправке SMS");
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
        // Удаление всех нецифровых символов
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Если начинается с +, удаляем его
        // +992901234567 → 992901234567
        return digitsOnly;
    }

    private static string GetOtpCacheKey(string phoneNumber)
    {
        return $"otp_{phoneNumber}";
    }
}

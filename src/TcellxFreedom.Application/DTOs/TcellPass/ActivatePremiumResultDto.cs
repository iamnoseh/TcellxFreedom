namespace TcellxFreedom.Application.DTOs.TcellPass;

public sealed record ActivatePremiumResultDto(
    DateTime ExpiresAt,
    string Message
);

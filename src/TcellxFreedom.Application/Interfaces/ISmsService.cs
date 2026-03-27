namespace TcellxFreedom.Application.Interfaces;

/// <summary>Объединяющий OTP интерфейс для регистрации зависимости в одном месте</summary>
public interface ISmsService : IOtpSender, IOtpVerifier;

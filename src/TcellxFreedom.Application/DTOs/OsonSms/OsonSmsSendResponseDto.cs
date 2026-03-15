namespace TcellxFreedom.Application.DTOs.OsonSms;

public sealed record OsonSmsSendResponseDto(string? MsgId, OsonSmsErrorDto? Error);

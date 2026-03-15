using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.OsonSms;

namespace TcellxFreedom.Application.Interfaces;

public interface IOsonSmsService
{
    Task<Response<OsonSmsSendResponseDto>> SendSmsAsync(string phoneNumber, string message);
    Task<Response<OsonSmsStatusResponseDto>> CheckSmsStatusAsync(string msgId);
    Task<Response<OsonSmsBalanceResponseDto>> CheckBalanceAsync();
}

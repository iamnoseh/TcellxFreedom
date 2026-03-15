using MediatR;
using TcellxFreedom.Application.DTOs;

namespace TcellxFreedom.Application.Features.Auth.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(string PhoneNumber, string OtpCode) : IRequest<AuthResponseDto>;

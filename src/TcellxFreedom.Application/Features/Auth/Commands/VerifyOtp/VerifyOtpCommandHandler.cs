using MediatR;
using TcellxFreedom.Application.DTOs;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;
using DomainUser = TcellxFreedom.Domain.Entities.User;

namespace TcellxFreedom.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler(
    ISmsService smsService,
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<VerifyOtpCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var isValid = await smsService.VerifyOtpAsync(request.PhoneNumber, request.OtpCode, cancellationToken);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid OTP code");

        var user = await userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);

        if (user == null)
        {
            user = DomainUser.Create(request.PhoneNumber);
            await userRepository.CreateAsync(user, cancellationToken);
        }

        var token = jwtTokenService.GenerateToken(user);

        var userDto = new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Balance
        );

        return new AuthResponseDto(token, userDto);
    }
}

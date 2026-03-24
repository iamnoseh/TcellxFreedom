using MediatR;
using TcellxFreedom.Application.DTOs;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;
using DomainUser = TcellxFreedom.Domain.Entities.User;

namespace TcellxFreedom.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler(
    IOtpVerifier otpVerifier,
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<VerifyOtpCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var isValid = await otpVerifier.VerifyOtpAsync(request.PhoneNumber, request.OtpCode, cancellationToken);
        if (!isValid)
            throw new UnauthorizedAccessException("Рамзи OTP нодуруст аст.");

        var user = await userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken)
                   ?? await CreateNewUserAsync(request.PhoneNumber, userRepository, cancellationToken);

        var token = jwtTokenService.GenerateToken(user);
        return new AuthResponseDto(token, new UserDto(user.Id, user.FirstName, user.LastName, user.PhoneNumber, user.Balance));
    }

    private static async Task<DomainUser> CreateNewUserAsync(string phone, IUserRepository repo, CancellationToken ct)
    {
        var user = DomainUser.Create(phone);
        await repo.CreateAsync(user, ct);
        return user;
    }
}

using MediatR;
using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(IOtpSender otpSender)
    : IRequestHandler<LoginCommand, Unit>
{
    public async Task<Unit> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        await otpSender.SendOtpAsync(request.PhoneNumber, cancellationToken);
        return Unit.Value;
    }
}

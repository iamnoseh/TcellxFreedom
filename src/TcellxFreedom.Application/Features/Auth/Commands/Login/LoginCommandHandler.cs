using MediatR;
using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Unit>
{
    private readonly ISmsService _smsService;

    public LoginCommandHandler(ISmsService smsService)
    {
        _smsService = smsService;
    }

    public async Task<Unit> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        await _smsService.SendOtpAsync(request.PhoneNumber, cancellationToken);
        return Unit.Value;
    }
}

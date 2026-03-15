using MediatR;

namespace TcellxFreedom.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string PhoneNumber) : IRequest<Unit>;

using FluentValidation;

namespace TcellxFreedom.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+992\d{9}$")
            .WithMessage("Phone number must be in format: +992XXXXXXXXX");
    }
}

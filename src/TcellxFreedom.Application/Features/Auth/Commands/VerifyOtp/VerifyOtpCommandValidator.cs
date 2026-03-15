using FluentValidation;

namespace TcellxFreedom.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+992\d{9}$");

        RuleFor(x => x.OtpCode)
            .NotEmpty()
            .Length(4)
            .Matches(@"^\d{4}$");
    }
}

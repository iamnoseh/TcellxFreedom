using FluentValidation;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.ClaimReward;

public sealed class ClaimRewardCommandValidator : AbstractValidator<ClaimRewardCommand>
{
    public ClaimRewardCommandValidator()
    {
        RuleFor(x => x.Level).InclusiveBetween(1, 20).WithMessage("Уровень должен быть от 1 до 20.");
    }
}

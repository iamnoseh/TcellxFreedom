using FluentValidation;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.ClaimReward;

public sealed class ClaimRewardCommandValidator : AbstractValidator<ClaimRewardCommand>
{
    public ClaimRewardCommandValidator()
    {
        RuleFor(x => x.Level).InclusiveBetween(1, 20).WithMessage("Дараҷа бояд байни 1 ва 20 бошад.");
    }
}

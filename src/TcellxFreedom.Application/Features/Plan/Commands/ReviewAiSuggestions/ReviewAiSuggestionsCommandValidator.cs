using FluentValidation;

namespace TcellxFreedom.Application.Features.Plan.Commands.ReviewAiSuggestions;

public sealed class ReviewAiSuggestionsCommandValidator : AbstractValidator<ReviewAiSuggestionsCommand>
{
    public ReviewAiSuggestionsCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("Plan ID is required.");

        RuleFor(x => x.Decisions)
            .NotEmpty().WithMessage("At least one decision is required.");
    }
}

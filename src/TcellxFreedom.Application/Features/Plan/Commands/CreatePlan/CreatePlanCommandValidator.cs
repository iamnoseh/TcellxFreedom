using FluentValidation;

namespace TcellxFreedom.Application.Features.Plan.Commands.CreatePlan;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.StartDate)
            .Must(d => d.Date >= DateTime.UtcNow.Date).WithMessage("Start date must be today or in the future.")
            .LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");

        RuleFor(x => x.EndDate)
            .Must((cmd, end) => end <= cmd.StartDate.AddDays(30)).WithMessage("Plan duration cannot exceed 30 days.");

        RuleFor(x => x.Tasks)
            .NotEmpty().WithMessage("At least one task is required.");

        RuleForEach(x => x.Tasks).ChildRules(task =>
        {
            task.RuleFor(t => t.Title)
                .NotEmpty().WithMessage("Task title is required.")
                .MaximumLength(300).WithMessage("Task title must not exceed 300 characters.");
        });

        RuleFor(x => x.UserTimeZone)
            .NotEmpty().WithMessage("User time zone is required.");
    }
}

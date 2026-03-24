using FluentValidation;

namespace TcellxFreedom.Application.Features.Plan.Commands.RescheduleTask;

public sealed class RescheduleTaskCommandValidator : AbstractValidator<RescheduleTaskCommand>
{
    public RescheduleTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.NewScheduledAt)
            .Must(d => d > DateTime.UtcNow).WithMessage("New scheduled time must be in the future.");
    }
}

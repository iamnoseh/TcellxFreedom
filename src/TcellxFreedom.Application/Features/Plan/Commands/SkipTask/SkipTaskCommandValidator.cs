using FluentValidation;

namespace TcellxFreedom.Application.Features.Plan.Commands.SkipTask;

public sealed class SkipTaskCommandValidator : AbstractValidator<SkipTaskCommand>
{
    public SkipTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
    }
}

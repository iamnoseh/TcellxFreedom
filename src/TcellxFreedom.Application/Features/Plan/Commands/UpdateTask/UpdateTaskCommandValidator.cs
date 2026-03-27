using FluentValidation;

namespace TcellxFreedom.Application.Features.Plan.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Идентификатор задачи обязателен.");

        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Название задачи не может быть пустым.")
                .MaximumLength(300).WithMessage("Название не может превышать 300 символов.");
        });

        When(x => x.ScheduledAt is not null, () =>
        {
            RuleFor(x => x.ScheduledAt)
                .Must(d => d!.Value > DateTime.UtcNow).WithMessage("Время должно быть в будущем.");
        });

        When(x => x.EstimatedMinutes is not null, () =>
        {
            RuleFor(x => x.EstimatedMinutes)
                .InclusiveBetween(1, 1440).WithMessage("Расчётное время должно быть от 1 до 1440 минут.");
        });
    }
}

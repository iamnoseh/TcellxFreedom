using FluentValidation;

namespace TcellxFreedom.Application.Features.Plan.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Идентификатори вазифа ҳатмист.");

        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Унвони вазифа холӣ буда наметавонад.")
                .MaximumLength(300).WithMessage("Унвон аз 300 аломат зиёд буда наметавонад.");
        });

        When(x => x.ScheduledAt is not null, () =>
        {
            RuleFor(x => x.ScheduledAt)
                .Must(d => d!.Value > DateTime.UtcNow).WithMessage("Вақти навбатӣ бояд дар оянда бошад.");
        });

        When(x => x.EstimatedMinutes is not null, () =>
        {
            RuleFor(x => x.EstimatedMinutes)
                .InclusiveBetween(1, 1440).WithMessage("Муддати тахминӣ бояд аз 1 то 1440 дақиқа бошад.");
        });
    }
}

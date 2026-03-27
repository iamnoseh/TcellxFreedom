using FluentValidation;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.CompleteTask;

public sealed class CompletePassTaskCommandValidator : AbstractValidator<CompletePassTaskCommand>
{
    public CompletePassTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Идентификатор задачи не может быть пустым.");
    }
}

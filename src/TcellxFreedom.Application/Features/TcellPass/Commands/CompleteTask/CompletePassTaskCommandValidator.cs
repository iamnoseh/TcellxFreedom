using FluentValidation;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.CompleteTask;

public sealed class CompletePassTaskCommandValidator : AbstractValidator<CompletePassTaskCommand>
{
    public CompletePassTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Идентификатори вазифа бояд пур бошад.");
    }
}

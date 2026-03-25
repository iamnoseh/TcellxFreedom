using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.CompleteTask;

public sealed record CompletePassTaskCommand(string UserId, Guid TaskId) : IRequest<Response<CompleteTaskResultDto>>;

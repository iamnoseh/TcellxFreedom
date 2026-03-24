using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.CompleteTask;

public sealed record CompleteTaskCommand(string UserId, Guid TaskId) : IRequest<Response<PlanTaskDto>>;

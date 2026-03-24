using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Plan;

namespace TcellxFreedom.Application.Features.Plan.Commands.SkipTask;

public sealed record SkipTaskCommand(string UserId, Guid TaskId) : IRequest<Response<PlanTaskDto>>;

using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetTodayTasks;

public sealed record GetTodayTasksQuery(string UserId) : IRequest<Response<List<UserDailyTaskDto>>>;

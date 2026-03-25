using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetTodayTasks;

public sealed class GetTodayTasksQueryHandler(IUserDailyTaskRepository dailyTaskRepository)
    : IRequestHandler<GetTodayTasksQuery, Response<List<UserDailyTaskDto>>>
{
    public async Task<Response<List<UserDailyTaskDto>>> Handle(GetTodayTasksQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tasks = await dailyTaskRepository.GetByUserAndDateAsync(request.UserId, today, cancellationToken);

        var dtos = tasks.Select(t => new UserDailyTaskDto(
            t.Id,
            t.Template.Title,
            t.Template.Description,
            t.Template.XpReward,
            t.Template.Category.ToString(),
            t.Template.IsPremiumOnly,
            t.Status.ToString(),
            t.CompletedAt
        )).ToList();

        return new Response<List<UserDailyTaskDto>>(dtos);
    }
}

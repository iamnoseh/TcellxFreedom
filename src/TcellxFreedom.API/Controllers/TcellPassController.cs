using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TcellxFreedom.Application.Features.TcellPass.Commands.ActivatePremium;
using TcellxFreedom.Application.Features.TcellPass.Commands.ClaimReward;
using TcellxFreedom.Application.Features.TcellPass.Commands.CompleteTask;
using TcellxFreedom.Application.Features.TcellPass.Queries.GetAllRewards;
using TcellxFreedom.Application.Features.TcellPass.Queries.GetLeaderboard;
using TcellxFreedom.Application.Features.TcellPass.Queries.GetMyPass;
using TcellxFreedom.Application.Features.TcellPass.Queries.GetTodayTasks;

namespace TcellxFreedom.API.Controllers;

[ApiController]
[Route("api/tcell-pass")]
[Authorize]
public sealed class TcellPassController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyPass(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetMyPassQuery(userId), ct);
        return Ok(result);
    }

    [HttpGet("tasks/today")]
    public async Task<IActionResult> GetTodayTasks(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetTodayTasksQuery(userId), ct);
        return Ok(result);
    }

    [HttpPost("tasks/{taskId:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid taskId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new CompletePassTaskCommand(userId, taskId), ct);
        return Ok(result);
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> GetAllRewards(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetAllRewardsQuery(userId), ct);
        return Ok(result);
    }

    [HttpPost("rewards/{level:int}/claim")]
    public async Task<IActionResult> ClaimReward(int level, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new ClaimRewardCommand(userId, level), ct);
        return Ok(result);
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int topN = 50, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetLeaderboardQuery(topN), ct);
        return Ok(result);
    }

    [HttpPost("premium/activate")]
    public async Task<IActionResult> ActivatePremium(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new ActivatePremiumCommand(userId), ct);
        return Ok(result);
    }
}

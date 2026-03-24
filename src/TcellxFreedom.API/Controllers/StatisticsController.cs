using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TcellxFreedom.Application.Features.Plan.Queries.GetStatistics;

namespace TcellxFreedom.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class StatisticsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStatistics([FromQuery] int weekCount = 4, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetStatisticsQuery(userId, weekCount), ct);
        return Ok(result);
    }
}

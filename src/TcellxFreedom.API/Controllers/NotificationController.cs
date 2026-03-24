using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TcellxFreedom.Application.Features.Plan.Queries.GetUpcomingNotifications;

namespace TcellxFreedom.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationController(IMediator mediator) : ControllerBase
{
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var fromDate = from ?? DateTime.UtcNow;
        var toDate = to ?? DateTime.UtcNow.AddHours(24);
        var result = await mediator.Send(new GetUpcomingNotificationsQuery(userId, fromDate, toDate), ct);
        return Ok(result);
    }
}

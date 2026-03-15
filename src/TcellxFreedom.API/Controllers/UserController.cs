using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TcellxFreedom.Application.DTOs;
using TcellxFreedom.Application.Features.User.Commands.UpdateProfile;
using TcellxFreedom.Application.Features.User.Queries.GetMe;

namespace TcellxFreedom.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UserController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetMeQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new UpdateProfileCommand(userId, dto.FirstName, dto.LastName), cancellationToken);
        return Ok(result);
    }
}

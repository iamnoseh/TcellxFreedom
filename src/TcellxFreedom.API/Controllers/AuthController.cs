using MediatR;
using Microsoft.AspNetCore.Mvc;
using TcellxFreedom.Application.DTOs;
using TcellxFreedom.Application.Features.Auth.Commands.Login;
using TcellxFreedom.Application.Features.Auth.Commands.VerifyOtp;

namespace TcellxFreedom.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(new LoginCommand(dto.PhoneNumber), cancellationToken);
        return Ok(new { message = "OTP sent successfully" });
    }

    [HttpPost("verify")]
    public async Task<ActionResult<AuthResponseDto>> Verify([FromBody] VerifyOtpDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new VerifyOtpCommand(dto.PhoneNumber, dto.OtpCode), cancellationToken);
        return Ok(result);
    }
}

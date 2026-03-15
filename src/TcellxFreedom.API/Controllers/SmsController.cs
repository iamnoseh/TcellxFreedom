using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SmsController : ControllerBase
{
    private readonly IOsonSmsService _osonSmsService;

    public SmsController(IOsonSmsService osonSmsService)
    {
        _osonSmsService = osonSmsService;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
    {
        var result = await _osonSmsService.CheckBalanceAsync();

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpGet("status/{msgId}")]
    public async Task<IActionResult> CheckStatus(string msgId, CancellationToken cancellationToken)
    {
        var result = await _osonSmsService.CheckSmsStatusAsync(msgId);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }
}

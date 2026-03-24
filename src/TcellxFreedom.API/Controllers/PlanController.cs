using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TcellxFreedom.Application.DTOs.Plan;
using TcellxFreedom.Application.Features.Plan.Commands.CompleteTask;
using TcellxFreedom.Application.Features.Plan.Commands.CreatePlan;
using TcellxFreedom.Application.Features.Plan.Commands.RescheduleTask;
using TcellxFreedom.Application.Features.Plan.Commands.ReviewAiSuggestions;
using TcellxFreedom.Application.Features.Plan.Commands.SkipTask;
using TcellxFreedom.Application.Features.Plan.Commands.CreatePlanFromChat;
using TcellxFreedom.Application.Features.Plan.Commands.UpdateTask;
using TcellxFreedom.Application.Features.Plan.Queries.GetAiSuggestions;
using TcellxFreedom.Application.Features.Plan.Queries.GetPlanById;
using TcellxFreedom.Application.Features.Plan.Queries.GetPlans;

namespace TcellxFreedom.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PlanController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequestDto dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new CreatePlanCommand(
            userId, dto.Title, dto.Description, dto.StartDate, dto.EndDate, dto.UserTimeZone, dto.Tasks), ct);
        return Ok(result);
    }

    [HttpPost("from-chat")]
    public async Task<IActionResult> CreatePlanFromChat([FromBody] CreatePlanFromChatRequestDto dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new CreatePlanFromChatCommand(userId, dto.Text, dto.Date, dto.UserTimeZone), ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetPlansQuery(userId), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlanById(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetPlanByIdQuery(userId, id), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/suggestions")]
    public async Task<IActionResult> GetAiSuggestions(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetAiSuggestionsQuery(userId, id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> ReviewAiSuggestions(Guid id, [FromBody] AiSuggestionReviewDto dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new ReviewAiSuggestionsCommand(userId, id, dto.Decisions), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/task/{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, Guid taskId, [FromBody] UpdateTaskRequestDto dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new UpdateTaskCommand(userId, taskId, dto.Title, dto.Description, dto.ScheduledAt, dto.EstimatedMinutes), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/task/{taskId:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid id, Guid taskId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new CompleteTaskCommand(userId, taskId), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/task/{taskId:guid}/skip")]
    public async Task<IActionResult> SkipTask(Guid id, Guid taskId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new SkipTaskCommand(userId, taskId), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/task/{taskId:guid}/reschedule")]
    public async Task<IActionResult> RescheduleTask(Guid id, Guid taskId, [FromBody] DateTime newScheduledAt, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new RescheduleTaskCommand(userId, taskId, newScheduledAt), ct);
        return Ok(result);
    }
}

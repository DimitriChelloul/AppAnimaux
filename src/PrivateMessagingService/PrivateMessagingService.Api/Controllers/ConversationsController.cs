using Microsoft.AspNetCore.Mvc;
using PrivateMessagingService.BLL.Models;
using PrivateMessagingService.BLL.Services;

namespace PrivateMessagingService.Api.Controllers;

[ApiController]
[Route("conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly IPrivateMessagingAppService _messaging;

    public ConversationsController(IPrivateMessagingAppService messaging) => _messaging = messaging;

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return Ok(await _messaging.GetMineAsync(userId, page, pageSize, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var result = await _messaging.CreateConversationAsync(userId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = result.Conversation.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var result = await _messaging.GetConversationAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return Ok(await _messaging.GetMessagesAsync(userId, id, page, pageSize, ct));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var result = await _messaging.SendMessageAsync(userId, id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, [FromBody] MarkConversationReadRequest? request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _messaging.MarkReadAsync(userId, id, request ?? new MarkConversationReadRequest(null), ct) ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }
}

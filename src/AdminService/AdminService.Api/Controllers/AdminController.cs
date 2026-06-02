using AdminService.BLL.Models;
using AdminService.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

[ApiController]
[Route("admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminAppService _admin;

    public AdminController(IAdminAppService admin) => _admin = admin;

    [HttpGet("moderation/queue")]
    public async Task<IActionResult> SearchQueue(
        [FromQuery] string? status,
        [FromQuery] string? sourceService,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await _admin.SearchQueueAsync(new ModerationQueueSearchRequest(status, sourceService, priority, page, pageSize), ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("moderation/queue/{id:long}")]
    public async Task<IActionResult> GetQueueItem(long id, CancellationToken ct)
    {
        try
        {
            var item = await _admin.GetQueueItemAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("moderation/queue")]
    public async Task<IActionResult> Enqueue([FromBody] CreateModerationQueueItemRequest request, CancellationToken ct)
    {
        try
        {
            var item = await _admin.EnqueueAsync(request, ct);
            return CreatedAtAction(nameof(GetQueueItem), new { id = item.Id }, item);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("moderation/queue/{id:long}/assign")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignModerationQueueItemRequest request, CancellationToken ct)
    {
        try
        {
            return await _admin.AssignQueueItemAsync(id, request, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("moderation/queue/{id:long}/close")]
    public async Task<IActionResult> Close(long id, [FromBody] CloseModerationQueueItemRequest request, CancellationToken ct)
    {
        try
        {
            return await _admin.CloseQueueItemAsync(id, request, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("moderation/actions")]
    public async Task<IActionResult> LogAction([FromBody] CreateModerationActionRequest request, CancellationToken ct)
    {
        if (!TryGetAdminUserId(out var adminUserId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return Ok(await _admin.LogModerationActionAsync(adminUserId, request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("moderation/actions")]
    public async Task<IActionResult> GetActions([FromQuery] string targetService, [FromQuery] string targetType, [FromQuery] Guid targetId, CancellationToken ct)
    {
        try
        {
            return Ok(await _admin.GetActionsForTargetAsync(targetService, targetType, targetId, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("sanctions")]
    public async Task<IActionResult> CreateSanction([FromBody] CreateUserSanctionRequest request, CancellationToken ct)
    {
        if (!TryGetAdminUserId(out var adminUserId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return Ok(await _admin.CreateSanctionAsync(adminUserId, request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("sanctions/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeSanction(Guid id, [FromBody] RevokeUserSanctionRequest request, CancellationToken ct)
    {
        try
        {
            return await _admin.RevokeSanctionAsync(id, request, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("users/{userId:guid}/sanctions/active")]
    public async Task<IActionResult> GetActiveSanctions(Guid userId, CancellationToken ct)
    {
        try
        {
            return Ok(await _admin.GetActiveSanctionsForUserAsync(userId, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("audit")]
    public async Task<IActionResult> LogAudit([FromBody] CreateAdminAuditLogRequest request, CancellationToken ct)
    {
        if (!TryGetAdminUserId(out var adminUserId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return Ok(await _admin.LogAuditAsync(adminUserId, request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    private bool TryGetAdminUserId(out Guid adminUserId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out adminUserId);
    }
}

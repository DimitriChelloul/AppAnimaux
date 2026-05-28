using MediaService.BLL.Models;
using MediaService.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Api.Controllers;

[ApiController]
[Route("media")]
public sealed class MediaController : ControllerBase
{
    private readonly IMediaAppService _media;

    public MediaController(IMediaAppService media) => _media = media;

    public sealed record AddUsageRequest(string ServiceName, string EntityType, Guid EntityId, string UsageType);

    [HttpPost("images")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<MediaFileResult>> UploadImage(
        IFormFile file,
        [FromForm] bool isPublic,
        [FromForm] string? serviceName,
        [FromForm] string? entityType,
        [FromForm] Guid? entityId,
        [FromForm] string? usageType,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        if (file.Length == 0)
        {
            return ValidationProblem("Image is empty.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _media.UploadImageAsync(
                new UploadMediaRequest(
                    userId,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream,
                    isPublic,
                    serviceName,
                    entityType,
                    entityId,
                    usageType),
                ct);

            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MediaFileResult>> Get(Guid id, CancellationToken ct)
    {
        var media = await _media.GetAsync(id, ct);
        return media is null ? NotFound() : Ok(media);
    }

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken ct)
    {
        TryGetUserId(out var userId);
        var content = await _media.GetContentAsync(id, userId == Guid.Empty ? null : userId, ct);
        return content is null ? NotFound() : PhysicalFile(content.FilePath, content.ContentType, content.FileName);
    }

    [HttpPost("{id:guid}/usages")]
    public async Task<IActionResult> AddUsage(Guid id, [FromBody] AddUsageRequest request, CancellationToken ct)
    {
        await _media.AddUsageAsync(id, request.ServiceName, request.EntityType, request.EntityId, request.UsageType, ct);
        return NoContent();
    }

    [HttpGet("usages")]
    public async Task<IActionResult> GetUsages([FromQuery] string serviceName, [FromQuery] string entityType, [FromQuery] Guid entityId, CancellationToken ct)
    {
        var usages = await _media.GetUsagesAsync(serviceName, entityType, entityId, ct);
        return Ok(usages);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }
}

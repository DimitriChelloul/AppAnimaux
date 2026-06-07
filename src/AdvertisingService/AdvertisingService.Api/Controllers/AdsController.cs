using AdvertisingService.BLL.Models;
using AdvertisingService.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingService.Api.Controllers;

[ApiController]
[Route("ads")]
public sealed class AdsController : ControllerBase
{
    private readonly IAdvertisingAppService _ads;

    public AdsController(IAdvertisingAppService ads) => _ads = ads;

    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateAdCampaignRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var advertiserUserId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var campaign = await _ads.CreateCampaignAsync(advertiserUserId, request, ct);
            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] Guid? advertiserUserId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var userFilter = IsAdmin() ? advertiserUserId : GetCurrentUserIdOrNull();
            return Ok(await _ads.GetCampaignsAsync(userFilter, status, page, pageSize, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("campaigns/{id:guid}")]
    public async Task<IActionResult> GetCampaign(Guid id, CancellationToken ct)
    {
        var campaign = await _ads.GetCampaignAsync(id, ct);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    [HttpPut("campaigns/{id:guid}/status/{status}")]
    public async Task<IActionResult> SetCampaignStatus(Guid id, string status, CancellationToken ct)
    {
        if (!TryGetUserId(out _))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return await _ads.SetCampaignStatusAsync(id, status, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("campaigns/{id:guid}/frequency")]
    public async Task<IActionResult> UpdateCampaignFrequency(Guid id, [FromBody] UpdateCampaignFrequencyRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out _))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return await _ads.UpdateCampaignFrequencyAsync(id, request, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("placements/{placement}/next")]
    public async Task<IActionResult> GetNextAd(
        string placement,
        [FromQuery] Guid? viewerUserId,
        [FromQuery] string? viewerKey,
        CancellationToken ct)
    {
        try
        {
            var userId = viewerUserId ?? GetCurrentUserIdOrNull();
            var anonymousViewerId = viewerKey ?? GetHeaderOrNull("X-Viewer-Key");
            var ad = await _ads.GetNextAdAsync(placement, userId, anonymousViewerId, ct);
            return ad is null ? NoContent() : Ok(ad);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("impressions")]
    public async Task<IActionResult> TrackImpression([FromBody] TrackAdInteractionRequest request, CancellationToken ct)
    {
        try
        {
            var interaction = await _ads.TrackImpressionAsync(request, ct);
            return interaction is null ? NotFound() : Ok(interaction);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("clicks")]
    public async Task<IActionResult> TrackClick([FromBody] TrackAdInteractionRequest request, CancellationToken ct)
    {
        try
        {
            var interaction = await _ads.TrackClickAsync(request, ct);
            return interaction is null ? NotFound() : Ok(interaction);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    private Guid? GetCurrentUserIdOrNull()
        => TryGetUserId(out var userId) ? userId : null;

    private string? GetHeaderOrNull(string name)
    {
        var value = Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }

    private bool IsAdmin()
    {
        var roles = Request.Headers["X-User-Roles"].ToString();
        return roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(role => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase));
    }
}

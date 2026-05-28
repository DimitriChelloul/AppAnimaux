using MediaService.BLL.Models;
using MediaService.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Api.Controllers;

[ApiController]
[Route("frontend-assets")]
public sealed class FrontendAssetsController : ControllerBase
{
    private readonly IMediaAppService _media;

    public FrontendAssetsController(IMediaAppService media) => _media = media;

    public sealed record UpsertFrontendAssetRequest(
        Guid MediaId,
        string AssetKey,
        string AssetType,
        string? Platform,
        string? Theme,
        string? Locale,
        string? DisplayName,
        string? Description,
        bool IsActive,
        int SortOrder);

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertFrontendAssetRequest request, CancellationToken ct)
    {
        try
        {
            var id = await _media.UpsertFrontendAssetAsync(
                new CreateFrontendAssetRequest(
                    request.MediaId,
                    request.AssetKey,
                    request.AssetType,
                    request.Platform ?? "all",
                    request.Theme ?? "default",
                    request.Locale,
                    request.DisplayName,
                    request.Description,
                    request.IsActive,
                    request.SortOrder),
                ct);

            return Ok(new { assetId = id });
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("{assetKey}")]
    public async Task<IActionResult> Get(
        string assetKey,
        [FromQuery] string platform = "all",
        [FromQuery] string theme = "default",
        [FromQuery] string? locale = null,
        CancellationToken ct = default)
    {
        var asset = await _media.GetFrontendAssetAsync(assetKey, platform, theme, locale, ct);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpGet("{assetKey}/content")]
    public async Task<IActionResult> GetContent(
        string assetKey,
        [FromQuery] string platform = "all",
        [FromQuery] string theme = "default",
        [FromQuery] string? locale = null,
        CancellationToken ct = default)
    {
        var asset = await _media.GetFrontendAssetAsync(assetKey, platform, theme, locale, ct);
        if (asset is null)
        {
            return NotFound();
        }

        return Redirect($"/media/{asset.MediaId}/content");
    }
}

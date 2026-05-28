using Microsoft.AspNetCore.Mvc;
using UserProfileService.BLL.Models;
using UserProfileService.BLL.Services;

namespace UserProfileService.Api.Controllers;

[ApiController]
[Route("profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IUserProfileAppService _profiles;

    public ProfilesController(IUserProfileAppService profiles) => _profiles = profiles;

    public sealed record SetProfileMediaRequest(Guid MediaId, string MediaUrl);

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var profile = await _profiles.GetMineAsync(userId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost("me")]
    public Task<IActionResult> CreateMine([FromBody] UpsertProfileRequest request, CancellationToken ct) => UpsertMine(request, ct);

    [HttpPut("me")]
    public async Task<IActionResult> UpsertMine([FromBody] UpsertProfileRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var profile = await _profiles.UpsertMineAsync(userId, request, ct);
            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("me/photos")]
    public async Task<IActionResult> AddPhoto([FromBody] ProfileMediaRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var media = await _profiles.AddPhotoAsync(userId, request, ct);
            return Ok(media);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("me/avatar")]
    public async Task<IActionResult> SetAvatar([FromBody] SetProfileMediaRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        await _profiles.SetAvatarAsync(userId, request.MediaId, request.MediaUrl, ct);
        return NoContent();
    }

    [HttpPut("me/banner")]
    public async Task<IActionResult> SetBanner([FromBody] SetProfileMediaRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        await _profiles.SetBannerAsync(userId, request.MediaId, request.MediaUrl, ct);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }
}

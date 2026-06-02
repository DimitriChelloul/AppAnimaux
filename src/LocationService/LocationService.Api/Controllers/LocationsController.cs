using LocationService.BLL.Models;
using LocationService.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocationService.Api.Controllers;

[ApiController]
[Route("locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationAppService _locations;

    public LocationsController(ILocationAppService locations) => _locations = locations;

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var location = await _locations.GetMineAsync(userId, ct);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpsertMine([FromBody] UpsertUserLocationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return Ok(await _locations.UpsertMineAsync(userId, request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("preferences/me")]
    public async Task<IActionResult> GetPreference(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var preference = await _locations.GetPreferenceAsync(userId, ct);
        return preference is null ? NotFound() : Ok(preference);
    }

    [HttpPut("preferences/me")]
    public async Task<IActionResult> UpsertPreference([FromBody] UpsertLocationPreferenceRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return Ok(await _locations.UpsertPreferenceAsync(userId, request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("distance")]
    public IActionResult CalculateDistance([FromBody] DistanceRequest request)
    {
        try
        {
            return Ok(_locations.CalculateDistance(request));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("nearby")]
    public async Task<IActionResult> FindNearby([FromBody] NearbyLocationsRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _locations.FindNearbyAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }
}

using Microsoft.AspNetCore.Mvc;
using ProfessionalService.BLL.Models;
using ProfessionalService.BLL.Services;

namespace ProfessionalService.Api.Controllers;

[ApiController]
[Route("professionals")]
public sealed class ProfessionalsController : ControllerBase
{
    private readonly IProfessionalAppService _professionals;

    public ProfessionalsController(IProfessionalAppService professionals) => _professionals = professionals;

    public sealed record VerifyRequest(bool IsVerified);

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] string? city,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Search(category, city, null, null, 10, page, pageSize, ct);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? category,
        [FromQuery] string? city,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double radiusKm = 10,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _professionals.SearchAsync(new ProfessionalSearchRequest(category, city, latitude, longitude, radiusKm, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var professional = await _professionals.GetMineAsync(userId, ct);
        return professional is null ? NotFound() : Ok(professional);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var professional = await _professionals.GetAsync(id, ct);
        return professional is null ? NotFound() : Ok(professional);
    }

    [HttpPost]
    public async Task<IActionResult> UpsertMine([FromBody] UpsertProfessionalRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var professional = await _professionals.UpsertMineAsync(userId, request, ct);
            return Ok(professional);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMine(Guid id, [FromBody] UpsertProfessionalRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var existing = await _professionals.GetAsync(id, ct);
        if (existing is null || existing.Professional.UserId != userId)
        {
            return NotFound();
        }

        try
        {
            return Ok(await _professionals.UpsertMineAsync(userId, request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMine(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _professionals.DeleteMineAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/services")]
    public async Task<IActionResult> AddService(Guid id, [FromBody] AddProfessionalServiceRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var service = await _professionals.AddServiceAsync(userId, id, request, ct);
        return service is null ? NotFound() : Ok(service);
    }

    [HttpPost("{id:guid}/photos")]
    public async Task<IActionResult> AddPhoto(Guid id, [FromBody] AddProfessionalPhotoRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var photo = await _professionals.AddPhotoAsync(userId, id, request, ct);
            return photo is null ? NotFound() : Ok(photo);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("{id:guid}/subscription")]
    public async Task<IActionResult> SetSubscription(Guid id, [FromBody] SetProfessionalSubscriptionRequest request, CancellationToken ct)
    {
        try
        {
            return await _professionals.SetSubscriptionAsync(id, request, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("{id:guid}/verified")]
    public async Task<IActionResult> SetVerified(Guid id, [FromBody] VerifyRequest request, CancellationToken ct)
    {
        return await _professionals.SetVerifiedAsync(id, request.IsVerified, ct) ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }
}

using HelpRequestService.BLL.Models;
using HelpRequestService.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace HelpRequestService.Api.Controllers;

[ApiController]
[Route("help-requests")]
public sealed class HelpRequestsController : ControllerBase
{
    private readonly IHelpRequestAppService _helpRequests;

    public HelpRequestsController(IHelpRequestAppService helpRequests) => _helpRequests = helpRequests;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHelpRequestRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var result = await _helpRequests.CreateAsync(userId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = result.HelpRequest.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return Ok(await _helpRequests.GetMineAsync(userId, ct));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? helpType,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double radiusKm = 10,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _helpRequests.SearchAsync(new SearchHelpRequestsRequest(helpType, latitude, longitude, radiusKm, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _helpRequests.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHelpRequestRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var result = await _helpRequests.UpdateAsync(userId, id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _helpRequests.PublishAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _helpRequests.CancelAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/in-progress")]
    public async Task<IActionResult> SetInProgress(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _helpRequests.SetInProgressAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _helpRequests.CompleteAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/proposals")]
    public async Task<IActionResult> AddProposal(Guid id, [FromBody] CreateHelpOfferRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var offer = await _helpRequests.AddOfferAsync(userId, id, request, ct);
        return offer is null ? NotFound() : Ok(offer);
    }

    [HttpPost("{id:guid}/proposals/{proposalId:guid}/accept")]
    public async Task<IActionResult> AcceptProposal(Guid id, Guid proposalId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var match = await _helpRequests.AcceptOfferAsync(userId, id, proposalId, ct);
        return match is null ? NotFound() : Ok(match);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }
}

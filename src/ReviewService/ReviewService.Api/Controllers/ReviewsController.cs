using Microsoft.AspNetCore.Mvc;
using ReviewService.BLL.Models;
using ReviewService.BLL.Services;

namespace ReviewService.Api.Controllers;

[ApiController]
[Route("reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewAppService _reviews;

    public ReviewsController(IReviewAppService reviews) => _reviews = reviews;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var review = await _reviews.CreateAsync(userId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = review.Id }, review);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeFlags = false, CancellationToken ct = default)
    {
        var review = await _reviews.GetAsync(id, includeFlags && IsAdmin(), ct);
        return review is null ? NotFound() : Ok(review);
    }

    [HttpGet("reviewees/{revieweeUserId:guid}")]
    public async Task<IActionResult> GetForReviewee(
        Guid revieweeUserId,
        [FromQuery] bool includeHidden = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var reviews = await _reviews.GetForRevieweeAsync(revieweeUserId, includeHidden && IsAdmin(), page, pageSize, ct);
        return Ok(reviews);
    }

    [HttpGet("reviewees/{revieweeUserId:guid}/summary")]
    public async Task<IActionResult> GetSummary(Guid revieweeUserId, CancellationToken ct)
    {
        return Ok(await _reviews.GetSummaryAsync(revieweeUserId, ct));
    }

    [HttpGet("mine/written")]
    public async Task<IActionResult> GetMineWritten([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return Ok(await _reviews.GetWrittenByAsync(userId, page, pageSize, ct));
    }

    [HttpPost("{id:guid}/flags")]
    public async Task<IActionResult> Flag(Guid id, [FromBody] FlagReviewRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var flag = await _reviews.FlagAsync(id, userId, request, ct);
            return flag is null ? NotFound() : Ok(flag);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("{id:guid}/moderation")]
    public async Task<IActionResult> Moderate(Guid id, [FromBody] ModerateReviewRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            return await _reviews.ModerateAsync(id, userId, request, ct) ? NoContent() : NotFound();
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

    private bool IsAdmin()
    {
        var roles = Request.Headers["X-User-Roles"].ToString();
        return roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(role => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase));
    }
}

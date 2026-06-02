using ForumService.BLL.Models;
using ForumService.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace ForumService.Api.Controllers;

[ApiController]
[Route("forum")]
public sealed class ForumController : ControllerBase
{
    private readonly IForumAppService _forum;

    public ForumController(IForumAppService forum) => _forum = forum;

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        return Ok(await _forum.GetCategoriesAsync(ct));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateForumCategoryRequest request, CancellationToken ct)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var category = await _forum.CreateCategoryAsync(request, ct);
            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await _forum.GetTopicsAsync(categoryId, IsAdmin() ? status : "open", page, pageSize, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateForumTopicRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var topic = await _forum.CreateTopicAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetTopic), new { id = topic.Id }, topic);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("topics/{id:guid}")]
    public async Task<IActionResult> GetTopic(Guid id, CancellationToken ct)
    {
        var topic = await _forum.GetTopicAsync(id, incrementViews: true, ct);
        return topic is null ? NotFound() : Ok(topic);
    }

    [HttpGet("topics/{id:guid}/posts")]
    public async Task<IActionResult> GetPosts(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        return Ok(await _forum.GetPostsAsync(id, includeHidden: IsAdmin(), page, pageSize, ct));
    }

    [HttpPost("topics/{id:guid}/posts")]
    public async Task<IActionResult> CreatePost(Guid id, [FromBody] CreateForumPostRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var post = await _forum.CreatePostAsync(userId, id, request, ct);
            return post is null ? NotFound() : Ok(post);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("posts/{postId:guid}")]
    public async Task<IActionResult> UpdatePost(Guid postId, [FromBody] UpdateForumPostRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            return await _forum.UpdatePostAsync(userId, postId, request, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpDelete("posts/{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _forum.DeletePostAsync(userId, postId, ct) ? NoContent() : NotFound();
    }

    [HttpPost("flags")]
    public async Task<IActionResult> Flag([FromBody] FlagForumContentRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var flag = await _forum.FlagAsync(userId, request, ct);
            return flag is null ? NotFound() : Ok(flag);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("moderation")]
    public async Task<IActionResult> Moderate([FromBody] ModerateForumContentRequest request, CancellationToken ct)
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
            return await _forum.ModerateAsync(userId, request, ct) ? NoContent() : NotFound();
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

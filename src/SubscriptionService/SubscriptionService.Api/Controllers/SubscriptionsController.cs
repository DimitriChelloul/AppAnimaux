namespace SubscriptionService.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using SubscriptionService.DAL.Queries;

[ApiController]
[Route("")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionQueries _q;
    public SubscriptionsController(SubscriptionQueries q) => _q = q;

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
        => Ok(await _q.GetPlansAsync());

    [HttpGet("subscriptions/me")]
    public async Task<IActionResult> GetMySubscription([FromQuery] Guid userId)
    {
        var sub = await _q.GetActiveSubscriptionAsync(userId);
        return sub is null ? NotFound() : Ok(sub);
    }
}


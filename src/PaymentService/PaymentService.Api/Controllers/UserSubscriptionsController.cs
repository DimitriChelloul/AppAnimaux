namespace PaymentService.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;

[ApiController]
[Route("api/user-subscriptions")]
public sealed class UserSubscriptionsController : PaymentControllerBase
{
    private readonly IUserSubscriptionService _subscriptions;
    private readonly ISubscriptionEntitlementService _entitlements;
    private readonly IApplePurchaseService _apple;
    private readonly IGooglePurchaseService _google;

    public UserSubscriptionsController(IUserSubscriptionService subscriptions, ISubscriptionEntitlementService entitlements, IApplePurchaseService apple, IGooglePurchaseService google)
    {
        _subscriptions = subscriptions;
        _entitlements = entitlements;
        _apple = apple;
        _google = google;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new { message = "Missing user identity." });
        return Ok(await _subscriptions.GetMineAsync(userId, ct));
    }

    [HttpGet("me/entitlements")]
    public async Task<IActionResult> GetEntitlements(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new { message = "Missing user identity." });
        return Ok(await _entitlements.GetForUserAsync(userId, ct));
    }

    [HttpPost("apple/validate")]
    public async Task<IActionResult> ValidateApple([FromBody] ValidateApplePurchaseDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new { message = "Missing user identity." });
        if (dto.UserId != userId) return Forbid();
        return Ok(await _apple.ValidateAsync(dto, ct));
    }

    [HttpPost("google/validate")]
    public async Task<IActionResult> ValidateGoogle([FromBody] ValidateGooglePurchaseDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new { message = "Missing user identity." });
        if (dto.UserId != userId) return Forbid();
        return Ok(await _google.ValidateAsync(dto, ct));
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new { message = "Missing user identity." });
        return Ok(await _subscriptions.CancelAsync(userId, ct));
    }
}

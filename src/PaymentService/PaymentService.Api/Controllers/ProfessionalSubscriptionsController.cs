namespace PaymentService.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.BLL.Options;

[ApiController]
[Route("api/professional-subscriptions")]
public sealed class ProfessionalSubscriptionsController : PaymentControllerBase
{
    private readonly IProfessionalSubscriptionService _subscriptions;
    private readonly ISubscriptionEntitlementService _entitlements;
    private readonly StripeOptions _stripeOptions;

    public ProfessionalSubscriptionsController(IProfessionalSubscriptionService subscriptions, ISubscriptionEntitlementService entitlements, IOptions<StripeOptions> stripeOptions)
    {
        _subscriptions = subscriptions;
        _entitlements = entitlements;
        _stripeOptions = stripeOptions.Value;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetProfessionalId(out var professionalId)) return Unauthorized(new { message = "Missing X-Professional-Id header." });
        return Ok(await _subscriptions.GetMineAsync(professionalId, ct));
    }

    [HttpGet("me/entitlements")]
    public async Task<IActionResult> GetEntitlements(CancellationToken ct)
    {
        if (!TryGetProfessionalId(out var professionalId)) return Unauthorized(new { message = "Missing X-Professional-Id header." });
        return Ok(await _entitlements.GetForProfessionalAsync(professionalId, ct));
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateProfessionalSubscriptionDto dto, CancellationToken ct)
    {
        if (!TryGetProfessionalId(out var professionalId)) return Unauthorized(new { message = "Missing X-Professional-Id header." });
        if (dto.ProfessionalId != professionalId) return Forbid();
        var request = dto with
        {
            SuccessUrl = string.IsNullOrWhiteSpace(dto.SuccessUrl) ? _stripeOptions.ProfessionalSuccessUrl : dto.SuccessUrl,
            CancelUrl = string.IsNullOrWhiteSpace(dto.CancelUrl) ? _stripeOptions.ProfessionalCancelUrl : dto.CancelUrl
        };
        return Ok(await _subscriptions.CreateCheckoutSessionAsync(request, ct));
    }

    [HttpPost("create-portal-session")]
    public async Task<IActionResult> CreatePortalSession(CancellationToken ct)
    {
        if (!TryGetProfessionalId(out var professionalId)) return Unauthorized(new { message = "Missing X-Professional-Id header." });
        return Ok(await _subscriptions.CreatePortalSessionAsync(professionalId, ct));
    }

    [HttpPost("change-plan")]
    public async Task<IActionResult> ChangePlan([FromBody] ChangeProfessionalPlanDto dto, CancellationToken ct)
    {
        if (!TryGetProfessionalId(out var professionalId)) return Unauthorized(new { message = "Missing X-Professional-Id header." });
        if (dto.ProfessionalId != professionalId) return Forbid();
        return Ok(await _subscriptions.ChangePlanAsync(dto, ct));
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken ct)
    {
        if (!TryGetProfessionalId(out var professionalId)) return Unauthorized(new { message = "Missing X-Professional-Id header." });
        return Ok(await _subscriptions.CancelAsync(professionalId, ct));
    }
}

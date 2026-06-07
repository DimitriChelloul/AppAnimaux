namespace PaymentService.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using PaymentService.BLL.Interfaces;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly ISubscriptionWebhookService _webhooks;
    public WebhooksController(ISubscriptionWebhookService webhooks) => _webhooks = webhooks;

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var result = await _webhooks.ProcessStripeAsync(payload, Request.Headers["Stripe-Signature"].ToString(), ct);
        return result.Processed ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("apple")]
    public async Task<IActionResult> Apple(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        return Ok(await _webhooks.ProcessAppleAsync(await reader.ReadToEndAsync(ct), ct));
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        return Ok(await _webhooks.ProcessGoogleAsync(await reader.ReadToEndAsync(ct), ct));
    }
}

namespace PaymentService.BLL.Providers;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.BLL.Options;
using PaymentService.Domain.Enums;

public sealed class StripeBillingService : IStripeBillingService
{
    private readonly HttpClient _http;
    private readonly StripeOptions _options;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(HttpClient http, IOptions<StripeOptions> options, ILogger<StripeBillingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(Guid professionalId, PlanCode planCode, string successUrl, string cancelUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            var id = $"cs_sim_{Guid.NewGuid():N}";
            return new StripeCheckoutSessionDto(id, $"https://stripe.local/checkout/{id}");
        }

        var values = new Dictionary<string, string>
        {
            ["mode"] = "subscription",
            ["success_url"] = successUrl,
            ["cancel_url"] = cancelUrl,
            ["client_reference_id"] = professionalId.ToString(),
            ["metadata[professionalId]"] = professionalId.ToString(),
            ["metadata[planCode]"] = planCode.ToString(),
            ["line_items[0][price]"] = planCode.ToString(),
            ["line_items[0][quantity]"] = "1"
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions")
        {
            Content = new FormUrlEncodedContent(values)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        using var response = await _http.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe checkout session creation failed: {Payload}", payload);
            throw new InvalidOperationException("Stripe checkout session creation failed.");
        }
        using var doc = JsonDocument.Parse(payload);
        return new StripeCheckoutSessionDto(
            doc.RootElement.GetProperty("id").GetString()!,
            doc.RootElement.GetProperty("url").GetString()!);
    }

    public async Task<StripePortalSessionDto> CreatePortalSessionAsync(string stripeCustomerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return new StripePortalSessionDto($"https://stripe.local/portal/{stripeCustomerId}");
        }

        var values = new Dictionary<string, string>
        {
            ["customer"] = stripeCustomerId,
            ["return_url"] = _options.CustomerPortalReturnUrl
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/billing_portal/sessions")
        {
            Content = new FormUrlEncodedContent(values)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        using var response = await _http.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Stripe portal session creation failed.");
        using var doc = JsonDocument.Parse(payload);
        return new StripePortalSessionDto(doc.RootElement.GetProperty("url").GetString()!);
    }

    public Task ChangePlanAsync(string stripeSubscriptionId, string stripePriceId, CancellationToken ct)
    {
        _logger.LogInformation("Stripe plan change requested for {SubscriptionId} to {PriceId}", stripeSubscriptionId, stripePriceId);
        return Task.CompletedTask;
    }

    public Task CancelSubscriptionAsync(string stripeSubscriptionId, bool cancelAtPeriodEnd, CancellationToken ct)
    {
        _logger.LogInformation("Stripe cancellation requested for {SubscriptionId}; cancelAtPeriodEnd={CancelAtPeriodEnd}", stripeSubscriptionId, cancelAtPeriodEnd);
        return Task.CompletedTask;
    }

    public bool IsValidWebhookSignature(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(signatureHeader)) return false;
        var timestamp = signatureHeader.Split(',').FirstOrDefault(x => x.StartsWith("t=", StringComparison.Ordinal))?.Substring(2);
        var signature = signatureHeader.Split(',').FirstOrDefault(x => x.StartsWith("v1=", StringComparison.Ordinal))?.Substring(3);
        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature)) return false;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(signature), hash);
    }
}

namespace PaymentService.BLL.Services;

using System.Text.Json;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public sealed class SubscriptionWebhookService : ISubscriptionWebhookService
{
    private readonly IWebhookEventRepository _webhooks;
    private readonly IStripeBillingService _stripe;
    private readonly IApplePurchaseService _apple;
    private readonly IGooglePurchaseService _google;
    private readonly IProfessionalSubscriptionService _professionals;
    private readonly ISubscriptionInvoiceRepository _invoices;

    public SubscriptionWebhookService(
        IWebhookEventRepository webhooks,
        IStripeBillingService stripe,
        IApplePurchaseService apple,
        IGooglePurchaseService google,
        IProfessionalSubscriptionService professionals,
        ISubscriptionInvoiceRepository invoices)
    {
        _webhooks = webhooks;
        _stripe = stripe;
        _apple = apple;
        _google = google;
        _professionals = professionals;
        _invoices = invoices;
    }

    public async Task<WebhookProcessResultDto> ProcessStripeAsync(string payload, string? signatureHeader, CancellationToken ct)
    {
        if (!_stripe.IsValidWebhookSignature(payload, signatureHeader))
        {
            return new WebhookProcessResultDto(false, "Invalid Stripe signature.");
        }

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "unknown" : "unknown";
        var eventId = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        var webhookId = await _webhooks.InsertAsync(new WebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = SubscriptionProvider.Stripe,
            EventType = type,
            ExternalEventId = eventId,
            Payload = payload
        }, ct);

        if (type is "checkout.session.completed" or "customer.subscription.created" or "customer.subscription.updated")
        {
            var obj = root.GetProperty("data").GetProperty("object");
            var professionalId = TryGetGuid(obj, "client_reference_id") ?? TryGetMetadataGuid(obj, "professionalId") ?? Guid.Empty;
            var planCode = TryGetMetadataPlan(obj, "planCode") ?? PlanCode.ProBasic;
            var stripeCustomerId = TryGetString(obj, "customer") ?? $"cus_unknown_{professionalId:N}";
            var stripeSubscriptionId = TryGetString(obj, "subscription") ?? TryGetString(obj, "id") ?? $"sub_unknown_{professionalId:N}";
            await _professionals.UpsertFromStripeAsync(professionalId, planCode, stripeCustomerId, stripeSubscriptionId, SubscriptionStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), ct);
        }
        else if (type == "customer.subscription.deleted")
        {
            var obj = root.GetProperty("data").GetProperty("object");
            var professionalId = TryGetMetadataGuid(obj, "professionalId") ?? Guid.Empty;
            await _professionals.UpsertFromStripeAsync(professionalId, PlanCode.ProFree, TryGetString(obj, "customer") ?? "", TryGetString(obj, "id") ?? "", SubscriptionStatus.Canceled, null, null, ct);
        }
        else if (type is "invoice.paid" or "invoice.payment_failed")
        {
            var obj = root.GetProperty("data").GetProperty("object");
            await _invoices.UpsertAsync(new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                SubscriptionOwnerType = SubscriptionOwnerType.Professional,
                SubscriptionId = Guid.Empty,
                Provider = SubscriptionProvider.Stripe,
                ExternalInvoiceId = TryGetString(obj, "id"),
                Amount = TryGetDecimalCents(obj, "amount_paid") ?? TryGetDecimalCents(obj, "amount_due") ?? 0,
                Currency = (TryGetString(obj, "currency") ?? "eur").ToUpperInvariant(),
                Status = type == "invoice.paid" ? InvoiceStatus.Paid : InvoiceStatus.Failed,
                InvoiceUrl = TryGetString(obj, "hosted_invoice_url"),
                PaidAt = type == "invoice.paid" ? DateTimeOffset.UtcNow : null
            }, ct);
        }

        await _webhooks.MarkProcessedAsync(webhookId, ct);
        return new WebhookProcessResultDto(true, $"Stripe webhook processed: {type}");
    }

    public async Task<WebhookProcessResultDto> ProcessAppleAsync(string payload, CancellationToken ct)
    {
        var id = await _webhooks.InsertAsync(new WebhookEvent { Id = Guid.NewGuid(), Provider = SubscriptionProvider.Apple, EventType = "apple.notification", Payload = payload }, ct);
        var result = await _apple.ProcessServerNotificationAsync(payload, ct);
        await _webhooks.MarkProcessedAsync(id, ct);
        return result;
    }

    public async Task<WebhookProcessResultDto> ProcessGoogleAsync(string payload, CancellationToken ct)
    {
        var id = await _webhooks.InsertAsync(new WebhookEvent { Id = Guid.NewGuid(), Provider = SubscriptionProvider.Google, EventType = "google.notification", Payload = payload }, ct);
        var result = await _google.ProcessServerNotificationAsync(payload, ct);
        await _webhooks.MarkProcessedAsync(id, ct);
        return result;
    }

    private static string? TryGetString(JsonElement obj, string property)
        => obj.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;

    private static Guid? TryGetGuid(JsonElement obj, string property)
        => Guid.TryParse(TryGetString(obj, property), out var id) ? id : null;

    private static Guid? TryGetMetadataGuid(JsonElement obj, string key)
        => obj.TryGetProperty("metadata", out var metadata) && metadata.TryGetProperty(key, out var value) && Guid.TryParse(value.GetString(), out var id) ? id : null;

    private static PlanCode? TryGetMetadataPlan(JsonElement obj, string key)
        => obj.TryGetProperty("metadata", out var metadata) && metadata.TryGetProperty(key, out var value) && Enum.TryParse<PlanCode>(value.GetString(), out var plan) ? plan : null;

    private static decimal? TryGetDecimalCents(JsonElement obj, string property)
        => obj.TryGetProperty(property, out var value) && value.TryGetDecimal(out var cents) ? cents / 100m : null;
}

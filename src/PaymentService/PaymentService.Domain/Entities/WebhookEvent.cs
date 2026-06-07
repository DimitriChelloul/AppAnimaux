namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class WebhookEvent
{
    public Guid Id { get; set; }
    public SubscriptionProvider Provider { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ExternalEventId { get; set; }
    public string Payload { get; set; } = "{}";
    public bool Processed { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

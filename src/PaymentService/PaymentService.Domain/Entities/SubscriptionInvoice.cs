namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class SubscriptionInvoice
{
    public Guid Id { get; set; }
    public SubscriptionOwnerType SubscriptionOwnerType { get; set; }
    public Guid SubscriptionId { get; set; }
    public SubscriptionProvider Provider { get; set; }
    public string? ExternalInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public InvoiceStatus Status { get; set; }
    public string? InvoiceUrl { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

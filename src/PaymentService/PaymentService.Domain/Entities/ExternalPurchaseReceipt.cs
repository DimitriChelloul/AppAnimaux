namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class ExternalPurchaseReceipt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SubscriptionProvider Provider { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? OriginalTransactionId { get; set; }
    public string? PurchaseToken { get; set; }
    public string RawReceipt { get; set; } = "{}";
    public string ValidationStatus { get; set; } = "pending";
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

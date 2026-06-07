namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class PaymentAuditLog
{
    public Guid Id { get; set; }
    public SubscriptionOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    public string Action { get; set; } = string.Empty;
    public SubscriptionProvider? Provider { get; set; }
    public string Details { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}

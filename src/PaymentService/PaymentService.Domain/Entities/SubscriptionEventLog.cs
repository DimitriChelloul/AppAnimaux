namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class SubscriptionEventLog
{
    public Guid Id { get; set; }
    public SubscriptionOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    public PaymentEventType EventType { get; set; }
    public string Details { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}

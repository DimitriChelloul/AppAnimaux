namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class SubscriptionPlan
{
    public Guid Id { get; set; }
    public PlanCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public SubscriptionOwnerType OwnerType { get; set; }
    public SubscriptionProvider? Provider { get; set; }
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string BillingPeriod { get; set; } = "month";
    public string? StripePriceId { get; set; }
    public string? AppleProductId { get; set; }
    public string? GoogleProductId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

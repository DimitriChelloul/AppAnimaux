namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class UserSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionProvider Provider { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTimeOffset? CurrentPeriodStart { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public bool AutoRenew { get; set; }
    public DateTimeOffset? CanceledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

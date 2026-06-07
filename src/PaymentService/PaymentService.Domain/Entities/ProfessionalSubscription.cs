namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class ProfessionalSubscription
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid PlanId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTimeOffset? CurrentPeriodStart { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public bool AutoRenew { get; set; }
    public DateTimeOffset? CanceledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

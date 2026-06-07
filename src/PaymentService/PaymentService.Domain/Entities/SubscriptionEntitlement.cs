namespace PaymentService.Domain.Entities;

public sealed class SubscriptionEntitlement
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

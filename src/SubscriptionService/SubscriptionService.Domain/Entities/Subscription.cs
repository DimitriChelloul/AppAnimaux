namespace SubscriptionService.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid PlanId { get; init; }

    public string Status { get; init; } = "active";
    public DateTimeOffset StartAt { get; init; }
    public DateTimeOffset CurrentPeriodStart { get; init; }
    public DateTimeOffset CurrentPeriodEnd { get; init; }
}


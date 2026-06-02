using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.Subscriptions;

public record SubscriptionActivatedEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserId { get; init; }

    public string PlanCode { get; init; } = default!;
    public PlanFeatures Features { get; init; } = new();

    public DateTimeOffset CurrentPeriodStart { get; init; }
    public DateTimeOffset CurrentPeriodEnd { get; init; }
    public Guid PlanId { get; set; }
}

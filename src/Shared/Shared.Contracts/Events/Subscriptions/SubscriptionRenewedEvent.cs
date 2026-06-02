using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.Subscriptions;

public record SubscriptionRenewedEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserId { get; init; }
    public string PlanCode { get; init; } = default!;
    public PlanFeatures Features { get; init; } = new();

    public DateTimeOffset NewPeriodStart { get; init; }
    public DateTimeOffset NewPeriodEnd { get; init; }
}

namespace PaymentService.Domain.Events;

using PaymentService.Domain.Enums;
using Shared.Contracts.Events.Abstractions;

public abstract record SubscriptionIntegrationEvent : IntegrationEvent
{
    public SubscriptionOwnerType OwnerType { get; init; }
    public Guid OwnerId { get; init; }
    public Guid SubscriptionId { get; init; }
    public PlanCode PlanCode { get; init; }
    public SubscriptionStatus Status { get; init; }
    public IReadOnlyDictionary<string, string> Entitlements { get; init; } = new Dictionary<string, string>();
}

public sealed record UserSubscriptionCreatedEvent : SubscriptionIntegrationEvent;
public sealed record UserSubscriptionRenewedEvent : SubscriptionIntegrationEvent;
public sealed record UserSubscriptionCanceledEvent : SubscriptionIntegrationEvent;
public sealed record UserSubscriptionExpiredEvent : SubscriptionIntegrationEvent;
public sealed record UserSubscriptionPaymentFailedEvent : SubscriptionIntegrationEvent;
public sealed record ProfessionalSubscriptionCreatedEvent : SubscriptionIntegrationEvent;
public sealed record ProfessionalSubscriptionRenewedEvent : SubscriptionIntegrationEvent;
public sealed record ProfessionalSubscriptionCanceledEvent : SubscriptionIntegrationEvent;
public sealed record ProfessionalSubscriptionExpiredEvent : SubscriptionIntegrationEvent;
public sealed record ProfessionalSubscriptionPaymentFailedEvent : SubscriptionIntegrationEvent;

public sealed record ProfessionalPlanChangedEvent : SubscriptionIntegrationEvent
{
    public PlanCode PreviousPlanCode { get; init; }
}

public sealed record SubscriptionEntitlementsChangedEvent : SubscriptionIntegrationEvent;

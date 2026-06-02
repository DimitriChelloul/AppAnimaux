using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.Payments;

public record PaymentSucceededEvent : IntegrationEvent
{
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }

    public string Provider { get; init; } = "stripe";
    public string? ProviderPaymentId { get; init; }
    public string? ProviderChargeId { get; init; }

    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";

    public string PurposeType { get; init; } = default!;
    public Guid? PurposeId { get; init; }

    public Guid? PaymentMethodId { get; init; }
    public string? PlanCode { get; set; }
}

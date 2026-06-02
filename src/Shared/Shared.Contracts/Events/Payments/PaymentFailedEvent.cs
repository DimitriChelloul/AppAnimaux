using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.Payments;

public record PaymentFailedEvent : IntegrationEvent
{
    public Guid PaymentIntentId { get; init; }
    public Guid UserId { get; init; }

    public string Provider { get; init; } = "stripe";
    public string? ProviderIntentId { get; init; }

    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";

    public string PurposeType { get; init; } = default!;
    public Guid? PurposeId { get; init; }

    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

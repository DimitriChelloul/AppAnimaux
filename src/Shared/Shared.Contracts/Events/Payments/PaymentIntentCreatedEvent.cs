using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Payments;

using Shared.Contracts.Events.Abstractions;

public record PaymentIntentCreatedEvent : IntegrationEvent
{
    public Guid PaymentIntentId { get; init; }
    public Guid UserId { get; init; }

    public string Provider { get; init; } = "stripe";
    public string ProviderIntentId { get; init; } = default!; // pi_...

    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";

    public string PurposeType { get; init; } = default!; // subscription/credits/ads_boost/...
    public Guid? PurposeId { get; init; }

    // Optionnel: utile pour client (mais ne stocke jamais le client_secret en clair en DB)
    public string? ClientSecretRef { get; init; } // ex: un hash/ref interne, pas le secret
}


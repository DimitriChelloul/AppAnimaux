using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Subscriptions;

using Shared.Contracts.Events.Abstractions;

public record SubscriptionCanceledEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserId { get; init; }

    public string PlanCode { get; init; } = default!;
    public string Status { get; init; } = "canceled"; // canceled/expired

    public DateTimeOffset CanceledAt { get; init; }
    public bool WasImmediate { get; init; } // true = cancel now, false = end of period
    public DateTimeOffset? EffectiveEndAt { get; init; } // si fin de période
    public string? Reason { get; init; }
}


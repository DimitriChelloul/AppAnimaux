using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Subscriptions;

using Shared.Contracts.Events.Abstractions;

public record SubscriptionPlanChangedEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserId { get; init; }

    public string OldPlanCode { get; init; } = default!;
    public string NewPlanCode { get; init; } = default!;

    public PlanFeatures NewFeatures { get; init; } = new();

    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;

    // true si le changement prend effet immédiatement
    public bool Immediate { get; init; } = true;
}


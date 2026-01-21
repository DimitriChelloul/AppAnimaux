using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Subscriptions;

using Shared.Contracts.Events.Abstractions;

public record SubscriptionCreatedEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserId { get; init; }

    public string PlanCode { get; init; } = default!; // FREE/BASIC/PREMIUM/PRO
    public string Status { get; init; } = "trialing"; // trialing/active/past_due/canceled/expired

    public DateTimeOffset StartAt { get; init; }
    public DateTimeOffset CurrentPeriodStart { get; init; }
    public DateTimeOffset CurrentPeriodEnd { get; init; }

    public bool CancelAtPeriodEnd { get; init; }
}


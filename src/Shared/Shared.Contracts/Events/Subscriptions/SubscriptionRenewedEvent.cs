using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Subscriptions
{
    

    using global::Shared.Contracts.Events.Abstractions;
    using Shared.Contracts.Events.Abstractions;

    public record SubscriptionRenewedEvent : IntegrationEvent
    {
        public Guid SubscriptionId { get; init; }
        public Guid UserId { get; init; }
        public string PlanCode { get; init; } = default!;
        public PlanFeatures Features { get; init; } = new();

        public DateTimeOffset NewPeriodStart { get; init; }
        public DateTimeOffset NewPeriodEnd { get; init; }
    }

}

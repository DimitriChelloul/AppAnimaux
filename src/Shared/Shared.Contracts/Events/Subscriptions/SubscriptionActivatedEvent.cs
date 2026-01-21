using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Subscriptions
{
    

    using global::Shared.Contracts.Events.Abstractions;
    using Shared.Contracts.Events.Abstractions;

    public record SubscriptionActivatedEvent : IntegrationEvent
    {
        public Guid SubscriptionId { get; init; }
        public Guid UserId { get; init; }

        public string PlanCode { get; init; } = default!; // FREE/BASIC/PREMIUM/PRO

        // Très pratique à inclure pour éviter d'appeler SubscriptionService partout
        public PlanFeatures Features { get; init; } = new();

        public DateTimeOffset CurrentPeriodStart { get; init; }
        public DateTimeOffset CurrentPeriodEnd { get; init; }
        public Guid PlanId { get; set; }
    }



}

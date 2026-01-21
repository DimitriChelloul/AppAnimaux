using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Payments
{
   

    using global::Shared.Contracts.Events.Abstractions;
    using Shared.Contracts.Events.Abstractions;

    public record PaymentSucceededEvent : IntegrationEvent
    {
        public Guid PaymentId { get; init; }
        public Guid UserId { get; init; }

        public string Provider { get; init; } = "stripe";
        public string? ProviderPaymentId { get; init; }   // pi_... / paypal txn id
        public string? ProviderChargeId { get; init; }    // ch_...

        public decimal Amount { get; init; }
        public string Currency { get; init; } = "EUR";

        // Lien métier
        public string PurposeType { get; init; } = default!; // subscription/credits/ads_boost/...
        public Guid? PurposeId { get; init; }                // ex: subscriptionId, reservationId...

        public Guid? PaymentMethodId { get; init; }
        public string PlanCode { get; set; }
    }

}

using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Credits
{
    

    using global::Shared.Contracts.Events.Abstractions;
    using Shared.Contracts.Events.Abstractions;

    public record CreditsGrantedEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public Guid WalletId { get; init; }

        public long Amount { get; init; } // +50, +150...
        public string ReasonCode { get; init; } = default!; // monthly_grant / purchase / adjustment

        public string? ReferenceType { get; init; } // subscription/payment/...
        public Guid? ReferenceId { get; init; }

        public long NewBalance { get; init; }
    }

}

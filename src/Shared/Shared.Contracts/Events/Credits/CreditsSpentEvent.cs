using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Credits;

using Shared.Contracts.Events.Abstractions;

public record CreditsSpentEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public Guid WalletId { get; init; }

    public long Amount { get; init; } // ex: 50 (montant dépensé)
    public string ReasonCode { get; init; } = default!; // boost_listing/message_fee/...

    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }

    public long NewBalance { get; init; }
}


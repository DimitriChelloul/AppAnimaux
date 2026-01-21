using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Credits;

using Shared.Contracts.Events.Abstractions;

public record CreditsReservationCanceledEvent : IntegrationEvent
{
    public Guid ReservationId { get; init; }
    public Guid UserId { get; init; }
    public Guid WalletId { get; init; }

    public long Amount { get; init; }
    public string? CancelReason { get; init; }

    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
}


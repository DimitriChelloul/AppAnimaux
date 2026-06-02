using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.Credits;

public record CreditsGrantedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public Guid WalletId { get; init; }

    public long Amount { get; init; }
    public string ReasonCode { get; init; } = default!;

    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }

    public long NewBalance { get; init; }
}

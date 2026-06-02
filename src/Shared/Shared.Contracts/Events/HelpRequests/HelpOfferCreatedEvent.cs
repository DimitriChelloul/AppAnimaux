using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.HelpRequests;

public sealed record HelpOfferCreatedEvent : IntegrationEvent
{
    public Guid HelpRequestId { get; init; }
    public Guid HelpOfferId { get; init; }
    public Guid RequesterUserId { get; init; }
    public Guid HelperUserId { get; init; }
    public string Title { get; init; } = "";
    public decimal? ProposedAmount { get; init; }
    public string Currency { get; init; } = "EUR";
}

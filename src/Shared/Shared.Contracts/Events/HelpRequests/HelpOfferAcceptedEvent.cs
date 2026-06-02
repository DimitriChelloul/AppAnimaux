using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.HelpRequests;

public sealed record HelpOfferAcceptedEvent : IntegrationEvent
{
    public Guid HelpRequestId { get; init; }
    public Guid HelpOfferId { get; init; }
    public Guid HelpMatchId { get; init; }
    public Guid RequesterUserId { get; init; }
    public Guid HelperUserId { get; init; }
    public string Title { get; init; } = "";
}

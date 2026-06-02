using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.HelpRequests;

public sealed record HelpMatchCompletedEvent : IntegrationEvent
{
    public Guid HelpRequestId { get; init; }
    public Guid RequesterUserId { get; init; }
    public string Title { get; init; } = "";
}

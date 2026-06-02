using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.HelpRequests;

public sealed record HelpRequestCreatedEvent : IntegrationEvent
{
    public Guid HelpRequestId { get; init; }
    public Guid RequesterUserId { get; init; }
    public Guid? PetId { get; init; }
    public string Title { get; init; } = "";
    public string HelpType { get; init; } = "";
    public string? City { get; init; }
    public string Status { get; init; } = "draft";
}

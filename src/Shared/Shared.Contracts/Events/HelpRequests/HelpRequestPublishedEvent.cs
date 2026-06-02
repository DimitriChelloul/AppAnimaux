using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.HelpRequests;

public sealed record HelpRequestPublishedEvent : IntegrationEvent
{
    public Guid HelpRequestId { get; init; }
    public Guid RequesterUserId { get; init; }
    public Guid? PetId { get; init; }
    public string Title { get; init; } = "";
    public string HelpType { get; init; } = "";
    public string? City { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

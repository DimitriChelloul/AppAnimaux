namespace AdvertisingService.Domain.Entities;

public sealed class AdInteraction
{
    public Guid Id { get; init; }
    public Guid CampaignId { get; init; }
    public Guid CreativeId { get; init; }
    public Guid? ViewerUserId { get; init; }
    public string? ViewerKey { get; init; }
    public string Placement { get; init; } = "";
    public string InteractionType { get; init; } = "";
    public string? LandingUrl { get; init; }
    public DateTimeOffset TrackedAt { get; init; }
}

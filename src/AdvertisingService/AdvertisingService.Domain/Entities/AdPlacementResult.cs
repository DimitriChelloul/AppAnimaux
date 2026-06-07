namespace AdvertisingService.Domain.Entities;

public sealed class AdPlacementResult
{
    public Guid CampaignId { get; init; }
    public Guid CreativeId { get; init; }
    public string Title { get; init; } = "";
    public string? Body { get; init; }
    public string? MediaUrl { get; init; }
    public string LandingUrl { get; init; } = "";
    public string Placement { get; init; } = "";
    public int? FrequencyCapPerUserDaily { get; init; }
    public int? CooldownMinutes { get; init; }
}

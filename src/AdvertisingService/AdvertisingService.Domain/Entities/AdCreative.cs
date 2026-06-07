namespace AdvertisingService.Domain.Entities;

public sealed class AdCreative
{
    public Guid Id { get; init; }
    public Guid CampaignId { get; init; }
    public string Title { get; init; } = "";
    public string? Body { get; init; }
    public string? MediaUrl { get; init; }
    public string LandingUrl { get; init; } = "";
    public string Placement { get; init; } = "";
    public int Weight { get; init; } = 1;
    public string Status { get; init; } = "active";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

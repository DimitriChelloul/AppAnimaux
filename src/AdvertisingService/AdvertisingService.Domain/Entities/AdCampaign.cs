namespace AdvertisingService.Domain.Entities;

public sealed class AdCampaign
{
    public Guid Id { get; init; }
    public Guid AdvertiserUserId { get; init; }
    public string Name { get; init; } = "";
    public string Objective { get; init; } = "";
    public decimal DailyBudget { get; init; }
    public decimal TotalBudget { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Status { get; init; } = "draft";
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public int? FrequencyCapPerUserDaily { get; init; }
    public int? CooldownMinutes { get; init; }
    public long ImpressionsCount { get; init; }
    public long ClicksCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

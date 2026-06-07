namespace AdvertisingService.BLL.Models;

public sealed record CreateAdCampaignRequest(
    string Name,
    string Objective,
    decimal DailyBudget,
    decimal TotalBudget,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    string CreativeTitle,
    string? CreativeBody,
    string? CreativeMediaUrl,
    string CreativeLandingUrl,
    string Placement,
    int Weight = 1,
    int? FrequencyCapPerUserDaily = 5,
    int? CooldownMinutes = 30);

public sealed record UpdateCampaignFrequencyRequest(
    int? FrequencyCapPerUserDaily,
    int? CooldownMinutes);

public sealed record TrackAdInteractionRequest(
    Guid CampaignId,
    Guid CreativeId,
    Guid? ViewerUserId,
    string? ViewerKey,
    string Placement,
    string? LandingUrl);

public sealed record AdCampaignResponse(
    Guid Id,
    Guid AdvertiserUserId,
    string Name,
    string Objective,
    decimal DailyBudget,
    decimal TotalBudget,
    string Currency,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    int? FrequencyCapPerUserDaily,
    int? CooldownMinutes,
    long ImpressionsCount,
    long ClicksCount,
    DateTimeOffset CreatedAt);

public sealed record AdPlacementResponse(
    Guid CampaignId,
    Guid CreativeId,
    string Title,
    string? Body,
    string? MediaUrl,
    string LandingUrl,
    string Placement,
    int? FrequencyCapPerUserDaily,
    int? CooldownMinutes);

public sealed record AdInteractionResponse(
    Guid Id,
    Guid CampaignId,
    Guid CreativeId,
    Guid? ViewerUserId,
    string? ViewerKey,
    string Placement,
    string InteractionType,
    string? LandingUrl,
    DateTimeOffset TrackedAt);

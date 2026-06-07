using System.Text.Json;
using AdvertisingService.BLL.Models;
using AdvertisingService.DAL.Repositories;
using AdvertisingService.Domain.Entities;
using Shared.Contracts.Events.Advertising;
using Shared.Contracts.Messaging;

namespace AdvertisingService.BLL.Services;

public sealed class AdvertisingAppService : IAdvertisingAppService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "draft",
        "active",
        "paused",
        "archived"
    };

    private readonly IAdvertisingRepository _ads;
    private readonly IOutboxRepository _outbox;

    public AdvertisingAppService(IAdvertisingRepository ads, IOutboxRepository outbox)
    {
        _ads = ads;
        _outbox = outbox;
    }

    public async Task<AdCampaignResponse> CreateCampaignAsync(Guid advertiserUserId, CreateAdCampaignRequest request, CancellationToken ct)
    {
        ValidateCampaign(request);

        var campaignId = Guid.NewGuid();
        var campaign = new AdCampaign
        {
            Id = campaignId,
            AdvertiserUserId = advertiserUserId,
            Name = request.Name.Trim(),
            Objective = request.Objective.Trim(),
            DailyBudget = request.DailyBudget,
            TotalBudget = request.TotalBudget,
            Currency = "EUR",
            Status = "draft",
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            FrequencyCapPerUserDaily = request.FrequencyCapPerUserDaily,
            CooldownMinutes = request.CooldownMinutes
        };

        var creative = new AdCreative
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Title = request.CreativeTitle.Trim(),
            Body = TrimToNull(request.CreativeBody),
            MediaUrl = TrimToNull(request.CreativeMediaUrl),
            LandingUrl = request.CreativeLandingUrl.Trim(),
            Placement = NormalizePlacement(request.Placement),
            Weight = request.Weight <= 0 ? 1 : request.Weight,
            Status = "active"
        };

        return ToResponse(await _ads.CreateCampaignAsync(campaign, creative, ct));
    }

    public async Task<IReadOnlyCollection<AdCampaignResponse>> GetCampaignsAsync(Guid? advertiserUserId, string? status, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1)
        {
            throw new ArgumentException("Page must be greater than 0.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100.");
        }

        if (!string.IsNullOrWhiteSpace(status) && !ValidStatuses.Contains(status))
        {
            throw new ArgumentException("Invalid campaign status.");
        }

        var rows = await _ads.GetCampaignsAsync(advertiserUserId, TrimToNull(status)?.ToLowerInvariant(), page, pageSize, ct);
        return rows.Select(ToResponse).ToArray();
    }

    public async Task<AdCampaignResponse?> GetCampaignAsync(Guid campaignId, CancellationToken ct)
    {
        var campaign = await _ads.GetCampaignAsync(campaignId, ct);
        return campaign is null ? null : ToResponse(campaign);
    }

    public async Task<AdPlacementResponse?> GetNextAdAsync(string placement, Guid? viewerUserId, string? anonymousViewerId, CancellationToken ct)
    {
        var ad = await _ads.GetNextAdAsync(NormalizePlacement(placement), BuildViewerKey(viewerUserId, anonymousViewerId), ct);
        return ad is null
            ? null
            : new AdPlacementResponse(
                ad.CampaignId,
                ad.CreativeId,
                ad.Title,
                ad.Body,
                ad.MediaUrl,
                ad.LandingUrl,
                ad.Placement,
                ad.FrequencyCapPerUserDaily,
                ad.CooldownMinutes);
    }

    public async Task<bool> SetCampaignStatusAsync(Guid campaignId, string status, CancellationToken ct)
    {
        if (!ValidStatuses.Contains(status))
        {
            throw new ArgumentException("Invalid campaign status.");
        }

        return await _ads.SetCampaignStatusAsync(campaignId, status.ToLowerInvariant(), ct);
    }

    public async Task<bool> UpdateCampaignFrequencyAsync(Guid campaignId, UpdateCampaignFrequencyRequest request, CancellationToken ct)
    {
        ValidateFrequency(request.FrequencyCapPerUserDaily, request.CooldownMinutes);
        return await _ads.UpdateCampaignFrequencyAsync(campaignId, request.FrequencyCapPerUserDaily, request.CooldownMinutes, ct);
    }

    public Task<AdInteractionResponse?> TrackImpressionAsync(TrackAdInteractionRequest request, CancellationToken ct)
        => TrackAsync(request, "impression", EventTypes.Advertising.ImpressionTracked, ct);

    public Task<AdInteractionResponse?> TrackClickAsync(TrackAdInteractionRequest request, CancellationToken ct)
        => TrackAsync(request, "click", EventTypes.Advertising.ClickTracked, ct);

    private async Task<AdInteractionResponse?> TrackAsync(TrackAdInteractionRequest request, string interactionType, string eventType, CancellationToken ct)
    {
        var interaction = new AdInteraction
        {
            Id = Guid.NewGuid(),
            CampaignId = request.CampaignId,
            CreativeId = request.CreativeId,
            ViewerUserId = request.ViewerUserId,
            ViewerKey = BuildViewerKey(request.ViewerUserId, request.ViewerKey),
            Placement = NormalizePlacement(request.Placement),
            InteractionType = interactionType,
            LandingUrl = TrimToNull(request.LandingUrl),
            TrackedAt = DateTimeOffset.UtcNow
        };

        var tracked = await _ads.TrackAsync(interaction, ct);
        if (tracked is null)
        {
            return null;
        }

        await PublishInteractionEventAsync(tracked, eventType, ct);
        return ToResponse(tracked);
    }

    private async Task PublishInteractionEventAsync(AdInteraction interaction, string eventType, CancellationToken ct)
    {
        var messageId = Guid.NewGuid();

        string payloadJson;
        if (interaction.InteractionType == "click")
        {
            var evt = new AdClickTrackedEvent
            {
                CampaignId = interaction.CampaignId,
                CreativeId = interaction.CreativeId,
                ViewerUserId = interaction.ViewerUserId,
                Placement = interaction.Placement,
                LandingUrl = interaction.LandingUrl,
                TrackedAt = interaction.TrackedAt
            };

            payloadJson = JsonSerializer.Serialize(
                new Shared.Contracts.Events.Abstractions.EventEnvelope<AdClickTrackedEvent>(
                    eventType,
                    EventTypes.V1,
                    evt,
                    DateTimeOffset.UtcNow,
                    messageId),
                JsonOptions);
        }
        else
        {
            var evt = new AdImpressionTrackedEvent
            {
                CampaignId = interaction.CampaignId,
                CreativeId = interaction.CreativeId,
                ViewerUserId = interaction.ViewerUserId,
                Placement = interaction.Placement,
                TrackedAt = interaction.TrackedAt
            };

            payloadJson = JsonSerializer.Serialize(
                new Shared.Contracts.Events.Abstractions.EventEnvelope<AdImpressionTrackedEvent>(
                    eventType,
                    EventTypes.V1,
                    evt,
                    DateTimeOffset.UtcNow,
                    messageId),
                JsonOptions);
        }

        await _outbox.AddAsync(messageId, eventType, payloadJson, "ad_interaction", interaction.Id, ct);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static void ValidateCampaign(CreateAdCampaignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Campaign name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Objective))
        {
            throw new ArgumentException("Campaign objective is required.");
        }

        if (request.DailyBudget <= 0 || request.TotalBudget <= 0)
        {
            throw new ArgumentException("Campaign budgets must be greater than 0.");
        }

        if (request.TotalBudget < request.DailyBudget)
        {
            throw new ArgumentException("Total budget must be greater than or equal to daily budget.");
        }

        if (request.EndsAt is not null && request.EndsAt < request.StartsAt)
        {
            throw new ArgumentException("Campaign end date must be after start date.");
        }

        ValidateFrequency(request.FrequencyCapPerUserDaily, request.CooldownMinutes);

        if (string.IsNullOrWhiteSpace(request.CreativeTitle))
        {
            throw new ArgumentException("Creative title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CreativeLandingUrl))
        {
            throw new ArgumentException("Creative landing URL is required.");
        }

        _ = NormalizePlacement(request.Placement);
    }

    private static void ValidateFrequency(int? frequencyCapPerUserDaily, int? cooldownMinutes)
    {
        if (frequencyCapPerUserDaily is < 1 or > 100)
        {
            throw new ArgumentException("Daily frequency cap must be between 1 and 100, or null for no cap.");
        }

        if (cooldownMinutes is < 1 or > 1440)
        {
            throw new ArgumentException("Cooldown minutes must be between 1 and 1440, or null for no cooldown.");
        }
    }

    private static string NormalizePlacement(string placement)
    {
        if (string.IsNullOrWhiteSpace(placement))
        {
            throw new ArgumentException("Placement is required.");
        }

        return placement.Trim().ToLowerInvariant();
    }

    private static string? BuildViewerKey(Guid? viewerUserId, string? anonymousViewerId)
    {
        if (viewerUserId is not null)
        {
            return $"user:{viewerUserId.Value:N}";
        }

        var normalized = TrimToNull(anonymousViewerId);
        if (normalized is null)
        {
            return null;
        }

        if (normalized.Length > 128)
        {
            throw new ArgumentException("Anonymous viewer key must be 128 characters or less.");
        }

        return $"anon:{normalized.ToLowerInvariant()}";
    }

    private static string? TrimToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AdCampaignResponse ToResponse(AdCampaign campaign)
        => new(
            campaign.Id,
            campaign.AdvertiserUserId,
            campaign.Name,
            campaign.Objective,
            campaign.DailyBudget,
            campaign.TotalBudget,
            campaign.Currency,
            campaign.Status,
            campaign.StartsAt,
            campaign.EndsAt,
            campaign.FrequencyCapPerUserDaily,
            campaign.CooldownMinutes,
            campaign.ImpressionsCount,
            campaign.ClicksCount,
            campaign.CreatedAt);

    private static AdInteractionResponse ToResponse(AdInteraction interaction)
        => new(
            interaction.Id,
            interaction.CampaignId,
            interaction.CreativeId,
            interaction.ViewerUserId,
            interaction.ViewerKey,
            interaction.Placement,
            interaction.InteractionType,
            interaction.LandingUrl,
            interaction.TrackedAt);
}

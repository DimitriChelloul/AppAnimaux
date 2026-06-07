using AdvertisingService.Domain.Entities;

namespace AdvertisingService.DAL.Repositories;

public interface IAdvertisingRepository
{
    Task<AdCampaign> CreateCampaignAsync(AdCampaign campaign, AdCreative creative, CancellationToken ct);
    Task<IReadOnlyCollection<AdCampaign>> GetCampaignsAsync(Guid? advertiserUserId, string? status, int page, int pageSize, CancellationToken ct);
    Task<AdCampaign?> GetCampaignAsync(Guid campaignId, CancellationToken ct);
    Task<AdPlacementResult?> GetNextAdAsync(string placement, string? viewerKey, CancellationToken ct);
    Task<bool> SetCampaignStatusAsync(Guid campaignId, string status, CancellationToken ct);
    Task<bool> UpdateCampaignFrequencyAsync(Guid campaignId, int? frequencyCapPerUserDaily, int? cooldownMinutes, CancellationToken ct);
    Task<AdInteraction?> TrackAsync(AdInteraction interaction, CancellationToken ct);
}

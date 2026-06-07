using AdvertisingService.BLL.Models;

namespace AdvertisingService.BLL.Services;

public interface IAdvertisingAppService
{
    Task<AdCampaignResponse> CreateCampaignAsync(Guid advertiserUserId, CreateAdCampaignRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<AdCampaignResponse>> GetCampaignsAsync(Guid? advertiserUserId, string? status, int page, int pageSize, CancellationToken ct);
    Task<AdCampaignResponse?> GetCampaignAsync(Guid campaignId, CancellationToken ct);
    Task<AdPlacementResponse?> GetNextAdAsync(string placement, Guid? viewerUserId, string? anonymousViewerId, CancellationToken ct);
    Task<bool> SetCampaignStatusAsync(Guid campaignId, string status, CancellationToken ct);
    Task<bool> UpdateCampaignFrequencyAsync(Guid campaignId, UpdateCampaignFrequencyRequest request, CancellationToken ct);
    Task<AdInteractionResponse?> TrackImpressionAsync(TrackAdInteractionRequest request, CancellationToken ct);
    Task<AdInteractionResponse?> TrackClickAsync(TrackAdInteractionRequest request, CancellationToken ct);
}

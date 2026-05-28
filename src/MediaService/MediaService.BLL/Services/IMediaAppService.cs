using MediaService.BLL.Models;
using MediaService.Domain.Entities;

namespace MediaService.BLL.Services;

public interface IMediaAppService
{
    Task<MediaFileResult> UploadImageAsync(UploadMediaRequest request, CancellationToken ct);
    Task<MediaFileResult?> GetAsync(Guid id, CancellationToken ct);
    Task<MediaContentResult?> GetContentAsync(Guid id, Guid? requesterUserId, CancellationToken ct);
    Task AddUsageAsync(Guid mediaId, string serviceName, string entityType, Guid entityId, string usageType, CancellationToken ct);
    Task<IReadOnlyCollection<MediaUsage>> GetUsagesAsync(string serviceName, string entityType, Guid entityId, CancellationToken ct);
    Task<Guid> UpsertFrontendAssetAsync(CreateFrontendAssetRequest request, CancellationToken ct);
    Task<FrontendAsset?> GetFrontendAssetAsync(string assetKey, string platform, string theme, string? locale, CancellationToken ct);
}

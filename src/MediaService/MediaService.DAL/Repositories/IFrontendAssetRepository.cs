using MediaService.Domain.Entities;

namespace MediaService.DAL.Repositories;

public interface IFrontendAssetRepository
{
    Task<Guid> UpsertAsync(FrontendAsset asset, CancellationToken ct);
    Task<FrontendAsset?> GetByKeyAsync(string assetKey, string platform, string theme, string? locale, CancellationToken ct);
}

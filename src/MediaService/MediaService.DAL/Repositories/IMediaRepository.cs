using MediaService.Domain.Entities;

namespace MediaService.DAL.Repositories;

public interface IMediaRepository
{
    Task InsertAsync(MediaFile media, CancellationToken ct);
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddUsageAsync(Guid mediaId, string serviceName, string entityType, Guid entityId, string usageType, CancellationToken ct);
    Task<IReadOnlyCollection<MediaUsage>> GetUsagesAsync(string serviceName, string entityType, Guid entityId, CancellationToken ct);
}

using UserProfileService.Domain.Entities;

namespace UserProfileService.DAL.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<UserProfile?> GetByIdAsync(Guid profileId, CancellationToken ct);
    Task<Guid> UpsertAsync(UserProfile profile, CancellationToken ct);
    Task<UserProfileMedia> AddMediaAsync(Guid profileId, Guid mediaId, string? mediaUrl, string usageType, int displayOrder, string? caption, bool isPrimary, CancellationToken ct);
    Task<IReadOnlyCollection<UserProfileMedia>> GetMediaAsync(Guid profileId, CancellationToken ct);
    Task SetAvatarAsync(Guid profileId, Guid mediaId, string mediaUrl, CancellationToken ct);
    Task SetBannerAsync(Guid profileId, Guid mediaId, string mediaUrl, CancellationToken ct);
}

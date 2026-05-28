using UserProfileService.BLL.Models;
using UserProfileService.Domain.Entities;

namespace UserProfileService.BLL.Services;

public interface IUserProfileAppService
{
    Task<UserProfileResponse?> GetMineAsync(Guid userId, CancellationToken ct);
    Task<UserProfileResponse> UpsertMineAsync(Guid userId, UpsertProfileRequest request, CancellationToken ct);
    Task<UserProfileMedia> AddPhotoAsync(Guid userId, ProfileMediaRequest request, CancellationToken ct);
    Task<bool> SetAvatarAsync(Guid userId, Guid mediaId, string mediaUrl, CancellationToken ct);
    Task<bool> SetBannerAsync(Guid userId, Guid mediaId, string mediaUrl, CancellationToken ct);
}

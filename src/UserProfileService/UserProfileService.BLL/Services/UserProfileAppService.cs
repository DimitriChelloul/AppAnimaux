using UserProfileService.BLL.Models;
using UserProfileService.DAL.Repositories;
using UserProfileService.Domain.Entities;

namespace UserProfileService.BLL.Services;

public sealed class UserProfileAppService : IUserProfileAppService
{
    private readonly IUserProfileRepository _profiles;

    public UserProfileAppService(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<UserProfileResponse?> GetMineAsync(Guid userId, CancellationToken ct)
    {
        var profile = await _profiles.GetByUserIdAsync(userId, ct);
        if (profile is null)
        {
            return null;
        }

        var media = await _profiles.GetMediaAsync(profile.Id, ct);
        return UserProfileResponse.From(profile, media);
    }

    public async Task<UserProfileResponse> UpsertMineAsync(Guid userId, UpsertProfileRequest request, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.");
        }

        var profileId = await _profiles.UpsertAsync(
            new UserProfile
            {
                UserId = userId,
                Username = NormalizeOptional(request.Username),
                DisplayName = NormalizeOptional(request.DisplayName),
                Bio = NormalizeOptional(request.Bio),
                Birthdate = request.Birthdate,
                City = NormalizeOptional(request.City),
                Country = NormalizeOptional(request.Country)
            },
            ct);

        var profile = await _profiles.GetByIdAsync(profileId, ct) ?? throw new InvalidOperationException("Profile could not be loaded.");
        var media = await _profiles.GetMediaAsync(profile.Id, ct);
        return UserProfileResponse.From(profile, media);
    }

    public async Task<UserProfileMedia> AddPhotoAsync(Guid userId, ProfileMediaRequest request, CancellationToken ct)
    {
        var profile = await EnsureProfileAsync(userId, ct);
        var usageType = NormalizeUsageType(request.UsageType);
        return await _profiles.AddMediaAsync(profile.Id, request.MediaId, request.MediaUrl, usageType, request.DisplayOrder, request.Caption, request.IsPrimary, ct);
    }

    public async Task<bool> SetAvatarAsync(Guid userId, Guid mediaId, string mediaUrl, CancellationToken ct)
    {
        var profile = await EnsureProfileAsync(userId, ct);
        await _profiles.AddMediaAsync(profile.Id, mediaId, mediaUrl, "avatar", 0, null, true, ct);
        await _profiles.SetAvatarAsync(profile.Id, mediaId, mediaUrl, ct);
        return true;
    }

    public async Task<bool> SetBannerAsync(Guid userId, Guid mediaId, string mediaUrl, CancellationToken ct)
    {
        var profile = await EnsureProfileAsync(userId, ct);
        await _profiles.AddMediaAsync(profile.Id, mediaId, mediaUrl, "banner", 0, null, true, ct);
        await _profiles.SetBannerAsync(profile.Id, mediaId, mediaUrl, ct);
        return true;
    }

    private async Task<UserProfile> EnsureProfileAsync(Guid userId, CancellationToken ct)
    {
        var profile = await _profiles.GetByUserIdAsync(userId, ct);
        if (profile is not null)
        {
            return profile;
        }

        var id = await _profiles.UpsertAsync(new UserProfile { UserId = userId }, ct);
        return await _profiles.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Profile could not be created.");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeUsageType(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "gallery" : value.Trim().ToLowerInvariant();
        if (normalized is not ("avatar" or "banner" or "gallery"))
        {
            throw new ArgumentException("Profile media usage type must be avatar, banner, or gallery.");
        }

        return normalized;
    }
}

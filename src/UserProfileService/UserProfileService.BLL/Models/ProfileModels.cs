using UserProfileService.Domain.Entities;

namespace UserProfileService.BLL.Models;

public sealed record UpsertProfileRequest(
    string? Username,
    string? DisplayName,
    string? Bio,
    DateTime? Birthdate,
    string? City,
    string? Country);

public sealed record ProfileMediaRequest(Guid MediaId, string? MediaUrl, string UsageType, int DisplayOrder, string? Caption, bool IsPrimary);

public sealed record UserProfileResponse(
    Guid Id,
    Guid UserId,
    string? Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? BannerUrl,
    DateTime? Birthdate,
    string? City,
    string? Country,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<UserProfileMedia> Media)
{
    public static UserProfileResponse From(UserProfile profile, IReadOnlyCollection<UserProfileMedia> media) =>
        new(profile.Id, profile.UserId, profile.Username, profile.DisplayName, profile.Bio, profile.AvatarUrl, profile.BannerUrl, profile.Birthdate, profile.City, profile.Country, profile.CreatedAt, profile.UpdatedAt, media);
}

namespace UserProfileService.Domain.Entities;

public sealed class UserProfile
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? Username { get; init; }
    public string? DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public string? BannerUrl { get; init; }
    public DateTime? Birthdate { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

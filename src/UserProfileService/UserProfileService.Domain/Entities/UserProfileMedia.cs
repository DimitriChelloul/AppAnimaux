namespace UserProfileService.Domain.Entities;

public sealed class UserProfileMedia
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public Guid MediaId { get; init; }
    public string? MediaUrl { get; init; }
    public string UsageType { get; init; } = "gallery";
    public int DisplayOrder { get; init; }
    public string? Caption { get; init; }
    public bool IsPrimary { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

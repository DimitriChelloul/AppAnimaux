namespace MediaService.Domain.Entities;

public sealed class FrontendAsset
{
    public Guid Id { get; init; }
    public Guid MediaId { get; init; }
    public string AssetKey { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public string Platform { get; init; } = "all";
    public string Theme { get; init; } = "default";
    public string? Locale { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? PublicUrl { get; init; }
    public string? ContentType { get; init; }
}

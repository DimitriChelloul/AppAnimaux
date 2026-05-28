namespace MediaService.Domain.Entities;

public sealed class MediaFile
{
    public Guid Id { get; init; }
    public Guid OwnerUserId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string? ChecksumSha256 { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationSeconds { get; init; }
    public string StorageProvider { get; init; } = "local";
    public string? StorageBucket { get; init; }
    public string StorageKey { get; init; } = string.Empty;
    public string? PublicUrl { get; init; }
    public bool IsPublic { get; init; }
    public string Status { get; init; } = "active";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
}

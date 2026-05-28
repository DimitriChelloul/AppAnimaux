using MediaService.Domain.Entities;

namespace MediaService.BLL.Models;

public sealed record UploadMediaRequest(
    Guid OwnerUserId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    bool IsPublic,
    string? ServiceName,
    string? EntityType,
    Guid? EntityId,
    string? UsageType);

public sealed record MediaFileResult(
    Guid Id,
    Guid OwnerUserId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? ChecksumSha256,
    string? PublicUrl,
    bool IsPublic,
    DateTimeOffset CreatedAt)
{
    public static MediaFileResult From(MediaFile media) =>
        new(
            media.Id,
            media.OwnerUserId,
            media.FileName,
            media.ContentType,
            media.SizeBytes,
            media.ChecksumSha256,
            media.PublicUrl,
            media.IsPublic,
            media.CreatedAt);
}

public sealed record MediaContentResult(string FilePath, string ContentType, string FileName);

public sealed record CreateFrontendAssetRequest(
    Guid MediaId,
    string AssetKey,
    string AssetType,
    string Platform,
    string Theme,
    string? Locale,
    string? DisplayName,
    string? Description,
    bool IsActive,
    int SortOrder);

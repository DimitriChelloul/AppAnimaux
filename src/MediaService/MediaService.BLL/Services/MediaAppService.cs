using System.Security.Cryptography;
using MediaService.BLL.Models;
using MediaService.BLL.Options;
using MediaService.DAL.Repositories;
using MediaService.Domain.Entities;
using Microsoft.Extensions.Options;

namespace MediaService.BLL.Services;

public sealed class MediaAppService : IMediaAppService
{
    private readonly IMediaRepository _media;
    private readonly IFrontendAssetRepository _assets;
    private readonly MediaStorageOptions _storage;

    public MediaAppService(IMediaRepository media, IFrontendAssetRepository assets, IOptions<MediaStorageOptions> storage)
    {
        _media = media;
        _assets = assets;
        _storage = storage.Value;
    }

    public async Task<MediaFileResult> UploadImageAsync(UploadMediaRequest request, CancellationToken ct)
    {
        ValidateImage(request);

        var id = Guid.NewGuid();
        var extension = GetSafeExtension(request.FileName, request.ContentType);
        var storageKey = Path.Combine("images", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), $"{id:N}{extension}")
            .Replace('\\', '/');
        var absolutePath = GetAbsolutePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var output = File.Create(absolutePath);
        using var sha = SHA256.Create();
        await using var hashingStream = new CryptoStream(output, sha, CryptoStreamMode.Write);
        await request.Content.CopyToAsync(hashingStream, ct);
        hashingStream.FlushFinalBlock();

        var info = new FileInfo(absolutePath);
        var checksum = Convert.ToHexString(sha.Hash ?? []);
        var publicUrl = BuildPublicUrl(id);

        var mediaFile = new MediaFile
        {
            Id = id,
            OwnerUserId = request.OwnerUserId,
            FileName = Path.GetFileName(request.FileName),
            ContentType = request.ContentType,
            SizeBytes = info.Length,
            ChecksumSha256 = checksum,
            StorageProvider = "local",
            StorageKey = storageKey,
            PublicUrl = publicUrl,
            IsPublic = request.IsPublic,
            Status = "active"
        };

        await _media.InsertAsync(mediaFile, ct);

        if (!string.IsNullOrWhiteSpace(request.ServiceName) &&
            !string.IsNullOrWhiteSpace(request.EntityType) &&
            request.EntityId.HasValue)
        {
            await _media.AddUsageAsync(
                id,
                NormalizeKey(request.ServiceName),
                NormalizeKey(request.EntityType),
                request.EntityId.Value,
                NormalizeKey(request.UsageType ?? "attachment"),
                ct);
        }

        var loaded = await _media.GetByIdAsync(id, ct) ?? mediaFile;
        return MediaFileResult.From(loaded);
    }

    public async Task<MediaFileResult?> GetAsync(Guid id, CancellationToken ct)
    {
        var media = await _media.GetByIdAsync(id, ct);
        return media is null ? null : MediaFileResult.From(media);
    }

    public async Task<MediaContentResult?> GetContentAsync(Guid id, Guid? requesterUserId, CancellationToken ct)
    {
        var media = await _media.GetByIdAsync(id, ct);
        if (media is null)
        {
            return null;
        }

        if (!media.IsPublic && requesterUserId != media.OwnerUserId)
        {
            return null;
        }

        var path = GetAbsolutePath(media.StorageKey);
        return File.Exists(path) ? new MediaContentResult(path, media.ContentType, media.FileName) : null;
    }

    public Task AddUsageAsync(Guid mediaId, string serviceName, string entityType, Guid entityId, string usageType, CancellationToken ct)
    {
        return _media.AddUsageAsync(mediaId, NormalizeKey(serviceName), NormalizeKey(entityType), entityId, NormalizeKey(usageType), ct);
    }

    public Task<IReadOnlyCollection<MediaUsage>> GetUsagesAsync(string serviceName, string entityType, Guid entityId, CancellationToken ct)
    {
        return _media.GetUsagesAsync(NormalizeKey(serviceName), NormalizeKey(entityType), entityId, ct);
    }

    public Task<Guid> UpsertFrontendAssetAsync(CreateFrontendAssetRequest request, CancellationToken ct)
    {
        ValidateFrontendAsset(request);

        var asset = new FrontendAsset
        {
            Id = Guid.NewGuid(),
            MediaId = request.MediaId,
            AssetKey = NormalizeAssetKey(request.AssetKey),
            AssetType = NormalizeKey(request.AssetType),
            Platform = NormalizeKey(request.Platform),
            Theme = NormalizeKey(request.Theme),
            Locale = string.IsNullOrWhiteSpace(request.Locale) ? null : request.Locale.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        return _assets.UpsertAsync(asset, ct);
    }

    public Task<FrontendAsset?> GetFrontendAssetAsync(string assetKey, string platform, string theme, string? locale, CancellationToken ct)
    {
        return _assets.GetByKeyAsync(
            NormalizeAssetKey(assetKey),
            NormalizeKey(string.IsNullOrWhiteSpace(platform) ? "all" : platform),
            NormalizeKey(string.IsNullOrWhiteSpace(theme) ? "default" : theme),
            string.IsNullOrWhiteSpace(locale) ? null : locale.Trim().ToLowerInvariant(),
            ct);
    }

    private void ValidateImage(UploadMediaRequest request)
    {
        if (request.OwnerUserId == Guid.Empty)
        {
            throw new ArgumentException("Owner user id is required.");
        }

        if (request.SizeBytes <= 0)
        {
            throw new ArgumentException("Image is empty.");
        }

        if (request.SizeBytes > _storage.MaxImageBytes)
        {
            throw new ArgumentException($"Image exceeds the maximum size of {_storage.MaxImageBytes} bytes.");
        }

        if (!_storage.AllowedImageContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unsupported image content type.");
        }
    }

    private static void ValidateFrontendAsset(CreateFrontendAssetRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AssetKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AssetType);

        if (request.MediaId == Guid.Empty)
        {
            throw new ArgumentException("Media id is required.");
        }
    }

    private static string GetSafeExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is ".jpg" or ".jpeg" or ".png" or ".webp")
        {
            return extension;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };
    }

    private string GetAbsolutePath(string storageKey)
    {
        return Path.GetFullPath(Path.Combine(_storage.RootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
    }

    private string BuildPublicUrl(Guid id)
    {
        var baseUrl = _storage.PublicBaseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? $"/media/{id}/content" : $"{baseUrl}/media/{id}/content";
    }

    private static string NormalizeKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeAssetKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().ToLowerInvariant();
    }
}

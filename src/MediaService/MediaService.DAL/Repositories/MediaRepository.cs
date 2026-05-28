using Dapper;
using MediaService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace MediaService.DAL.Repositories;

public sealed class MediaRepository : IMediaRepository
{
    private readonly IDbConnectionFactory _db;

    public MediaRepository(IDbConnectionFactory db) => _db = db;

    public async Task InsertAsync(MediaFile media, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            INSERT INTO media_files (
                id, owner_user_id, file_name, content_type, size_bytes,
                checksum_sha256, width, height, duration_seconds,
                storage_provider, storage_bucket, storage_key, public_url,
                is_public, status
            )
            VALUES (
                @Id, @OwnerUserId, @FileName, @ContentType, @SizeBytes,
                @ChecksumSha256, @Width, @Height, @DurationSeconds,
                @StorageProvider, @StorageBucket, @StorageKey, @PublicUrl,
                @IsPublic, @Status
            )
            """,
            media);
    }

    public async Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<MediaFile>(
            """
            SELECT
                id AS Id,
                owner_user_id AS OwnerUserId,
                file_name AS FileName,
                content_type AS ContentType,
                size_bytes AS SizeBytes,
                checksum_sha256 AS ChecksumSha256,
                width AS Width,
                height AS Height,
                duration_seconds AS DurationSeconds,
                storage_provider AS StorageProvider,
                storage_bucket AS StorageBucket,
                storage_key AS StorageKey,
                public_url AS PublicUrl,
                is_public AS IsPublic,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                deleted_at AS DeletedAt
            FROM media_files
            WHERE id = @Id
              AND deleted_at IS NULL
              AND status = 'active'
            """,
            new { Id = id });
    }

    public async Task AddUsageAsync(Guid mediaId, string serviceName, string entityType, Guid entityId, string usageType, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            INSERT INTO media_usages (media_id, service_name, entity_type, entity_id, usage_type)
            VALUES (@MediaId, @ServiceName, @EntityType, @EntityId, @UsageType)
            ON CONFLICT (media_id, service_name, entity_type, entity_id, usage_type) DO NOTHING
            """,
            new { MediaId = mediaId, ServiceName = serviceName, EntityType = entityType, EntityId = entityId, UsageType = usageType });
    }

    public async Task<IReadOnlyCollection<MediaUsage>> GetUsagesAsync(string serviceName, string entityType, Guid entityId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var usages = await cn.QueryAsync<MediaUsage>(
            """
            SELECT
                id AS Id,
                media_id AS MediaId,
                service_name AS ServiceName,
                entity_type AS EntityType,
                entity_id AS EntityId,
                usage_type AS UsageType,
                created_at AS CreatedAt
            FROM media_usages
            WHERE service_name = @ServiceName
              AND entity_type = @EntityType
              AND entity_id = @EntityId
            ORDER BY created_at
            """,
            new { ServiceName = serviceName, EntityType = entityType, EntityId = entityId });

        return usages.ToArray();
    }
}

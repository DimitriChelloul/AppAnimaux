using Dapper;
using Shared.Persistence.Abstractions;
using UserProfileService.Domain.Entities;

namespace UserProfileService.DAL.Repositories;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly IDbConnectionFactory _db;

    public UserProfileRepository(IDbConnectionFactory db) => _db = db;

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<UserProfile>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                username AS Username,
                display_name AS DisplayName,
                bio AS Bio,
                avatar_url AS AvatarUrl,
                banner_url AS BannerUrl,
                birthdate::timestamp AS Birthdate,
                city AS City,
                country AS Country,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM user_profiles
            WHERE user_id = @UserId
            """,
            new { UserId = userId });
    }

    public async Task<UserProfile?> GetByIdAsync(Guid profileId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<UserProfile>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                username AS Username,
                display_name AS DisplayName,
                bio AS Bio,
                avatar_url AS AvatarUrl,
                banner_url AS BannerUrl,
                birthdate::timestamp AS Birthdate,
                city AS City,
                country AS Country,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM user_profiles
            WHERE id = @ProfileId
            """,
            new { ProfileId = profileId });
    }

    public async Task<Guid> UpsertAsync(UserProfile profile, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO user_profiles (
                user_id, username, display_name, bio, birthdate, city, country
            )
            VALUES (
                @UserId, @Username, @DisplayName, @Bio, @Birthdate, @City, @Country
            )
            ON CONFLICT (user_id)
            DO UPDATE SET
                username = EXCLUDED.username,
                display_name = EXCLUDED.display_name,
                bio = EXCLUDED.bio,
                birthdate = EXCLUDED.birthdate,
                city = EXCLUDED.city,
                country = EXCLUDED.country,
                updated_at = now()
            RETURNING id
            """,
            profile);
    }

    public async Task<UserProfileMedia> AddMediaAsync(Guid profileId, Guid mediaId, string? mediaUrl, string usageType, int displayOrder, string? caption, bool isPrimary, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        using var tx = cn.BeginTransaction();
        if (isPrimary)
        {
            await cn.ExecuteAsync(
                "UPDATE user_profile_media SET is_primary = false WHERE profile_id = @ProfileId AND usage_type = @UsageType",
                new { ProfileId = profileId, UsageType = usageType },
                tx);
        }

        var media = await cn.QuerySingleAsync<UserProfileMedia>(
            """
            INSERT INTO user_profile_media (
                profile_id, media_id, media_url, usage_type, display_order, caption, is_primary
            )
            VALUES (@ProfileId, @MediaId, @MediaUrl, @UsageType, @DisplayOrder, @Caption, @IsPrimary)
            ON CONFLICT (profile_id, media_id, usage_type)
            DO UPDATE SET
                media_url = EXCLUDED.media_url,
                display_order = EXCLUDED.display_order,
                caption = EXCLUDED.caption,
                is_primary = EXCLUDED.is_primary
            RETURNING
                id AS Id,
                profile_id AS ProfileId,
                media_id AS MediaId,
                media_url AS MediaUrl,
                usage_type AS UsageType,
                display_order AS DisplayOrder,
                caption AS Caption,
                is_primary AS IsPrimary,
                created_at AS CreatedAt
            """,
            new { ProfileId = profileId, MediaId = mediaId, MediaUrl = mediaUrl, UsageType = usageType, DisplayOrder = displayOrder, Caption = caption, IsPrimary = isPrimary },
            tx);

        tx.Commit();
        return media;
    }

    public async Task<IReadOnlyCollection<UserProfileMedia>> GetMediaAsync(Guid profileId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var media = await cn.QueryAsync<UserProfileMedia>(
            """
            SELECT
                id AS Id,
                profile_id AS ProfileId,
                media_id AS MediaId,
                media_url AS MediaUrl,
                usage_type AS UsageType,
                display_order AS DisplayOrder,
                caption AS Caption,
                is_primary AS IsPrimary,
                created_at AS CreatedAt
            FROM user_profile_media
            WHERE profile_id = @ProfileId
            ORDER BY usage_type, display_order, created_at
            """,
            new { ProfileId = profileId });

        return media.ToArray();
    }

    public async Task SetAvatarAsync(Guid profileId, Guid mediaId, string mediaUrl, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            UPDATE user_profiles
            SET avatar_url = @MediaUrl,
                updated_at = now()
            WHERE id = @ProfileId
            """,
            new { ProfileId = profileId, MediaUrl = mediaUrl });
    }

    public async Task SetBannerAsync(Guid profileId, Guid mediaId, string mediaUrl, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            UPDATE user_profiles
            SET banner_url = @MediaUrl,
                updated_at = now()
            WHERE id = @ProfileId
            """,
            new { ProfileId = profileId, MediaUrl = mediaUrl });
    }
}

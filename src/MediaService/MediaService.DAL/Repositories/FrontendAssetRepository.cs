using Dapper;
using MediaService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace MediaService.DAL.Repositories;

public sealed class FrontendAssetRepository : IFrontendAssetRepository
{
    private readonly IDbConnectionFactory _db;

    public FrontendAssetRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> UpsertAsync(FrontendAsset asset, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO frontend_assets (
                id, media_id, asset_key, asset_type, platform, theme,
                locale, display_name, description, is_active, sort_order
            )
            VALUES (
                @Id, @MediaId, @AssetKey, @AssetType, @Platform, @Theme,
                @Locale, @DisplayName, @Description, @IsActive, @SortOrder
            )
            ON CONFLICT (asset_key, platform, theme, COALESCE(locale, ''))
            DO UPDATE SET
                media_id = EXCLUDED.media_id,
                asset_type = EXCLUDED.asset_type,
                display_name = EXCLUDED.display_name,
                description = EXCLUDED.description,
                is_active = EXCLUDED.is_active,
                sort_order = EXCLUDED.sort_order,
                updated_at = now()
            RETURNING id
            """,
            asset);
    }

    public async Task<FrontendAsset?> GetByKeyAsync(string assetKey, string platform, string theme, string? locale, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QueryFirstOrDefaultAsync<FrontendAsset>(
            """
            SELECT
                fa.id AS Id,
                fa.media_id AS MediaId,
                fa.asset_key AS AssetKey,
                fa.asset_type AS AssetType,
                fa.platform AS Platform,
                fa.theme AS Theme,
                fa.locale AS Locale,
                fa.display_name AS DisplayName,
                fa.description AS Description,
                fa.is_active AS IsActive,
                fa.sort_order AS SortOrder,
                fa.created_at AS CreatedAt,
                fa.updated_at AS UpdatedAt,
                mf.public_url AS PublicUrl,
                mf.content_type AS ContentType
            FROM frontend_assets fa
            INNER JOIN media_files mf ON mf.id = fa.media_id
            WHERE fa.asset_key = @AssetKey
              AND fa.is_active = true
              AND fa.platform IN (@Platform, 'all')
              AND fa.theme IN (@Theme, 'default')
              AND (fa.locale = @Locale OR fa.locale IS NULL)
              AND mf.status = 'active'
              AND mf.deleted_at IS NULL
            ORDER BY
                CASE WHEN fa.platform = @Platform THEN 0 ELSE 1 END,
                CASE WHEN fa.theme = @Theme THEN 0 ELSE 1 END,
                CASE WHEN fa.locale = @Locale THEN 0 ELSE 1 END,
                fa.sort_order,
                fa.updated_at DESC
            LIMIT 1
            """,
            new { AssetKey = assetKey, Platform = platform, Theme = theme, Locale = locale });
    }
}

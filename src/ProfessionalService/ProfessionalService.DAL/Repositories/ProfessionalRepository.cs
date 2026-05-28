using Dapper;
using ProfessionalService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace ProfessionalService.DAL.Repositories;

public sealed class ProfessionalRepository : IProfessionalRepository
{
    private readonly IDbConnectionFactory _db;

    public ProfessionalRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> UpsertAsync(Professional p, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO professionals (
                user_id, business_name, category, description, address, city, postal_code,
                latitude, longitude, phone, email, website, subscription_plan, subscription_status
            )
            VALUES (
                @UserId, @BusinessName, @Category, @Description, @Address, @City, @PostalCode,
                @Latitude, @Longitude, @Phone, @Email, @Website, @SubscriptionPlan, @SubscriptionStatus
            )
            ON CONFLICT (user_id)
            DO UPDATE SET
                business_name = EXCLUDED.business_name,
                category = EXCLUDED.category,
                description = EXCLUDED.description,
                address = EXCLUDED.address,
                city = EXCLUDED.city,
                postal_code = EXCLUDED.postal_code,
                latitude = EXCLUDED.latitude,
                longitude = EXCLUDED.longitude,
                phone = EXCLUDED.phone,
                email = EXCLUDED.email,
                website = EXCLUDED.website,
                updated_at = now()
            RETURNING id
            """,
            p);
    }

    public Task<Professional?> GetByIdAsync(Guid id, CancellationToken ct) => GetSingleAsync("id = @Id", new { Id = id });

    public Task<Professional?> GetByUserIdAsync(Guid userId, CancellationToken ct) => GetSingleAsync("user_id = @UserId", new { UserId = userId });

    public async Task<IReadOnlyCollection<Professional>> SearchAsync(string? category, string? city, double? latitude, double? longitude, double radiusKm, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;
        var hasGeo = latitude.HasValue && longitude.HasValue;

        var rows = await cn.QueryAsync<Professional>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                business_name AS BusinessName,
                category AS Category,
                description AS Description,
                address AS Address,
                city AS City,
                postal_code AS PostalCode,
                latitude AS Latitude,
                longitude AS Longitude,
                phone AS Phone,
                email AS Email,
                website AS Website,
                subscription_plan AS SubscriptionPlan,
                subscription_status AS SubscriptionStatus,
                is_verified AS IsVerified,
                average_rating AS AverageRating,
                review_count AS ReviewCount,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM professionals
            WHERE subscription_status = 'active'
              AND (@Category IS NULL OR category = @Category)
              AND (@City IS NULL OR city ILIKE @City)
              AND (
                    @HasGeo = false
                    OR (
                        latitude IS NOT NULL
                        AND longitude IS NOT NULL
                        AND (
                            6371 * acos(
                                least(
                                    1,
                                    greatest(
                                        -1,
                                        cos(radians(@Latitude)) * cos(radians(latitude)) *
                                        cos(radians(longitude) - radians(@Longitude)) +
                                        sin(radians(@Latitude)) * sin(radians(latitude))
                                    )
                                )
                            )
                        ) <= @RadiusKm
                    )
                  )
            ORDER BY
                CASE subscription_plan
                    WHEN 'professional_premium_plus' THEN 0
                    WHEN 'professional_premium' THEN 1
                    WHEN 'professional_basic' THEN 2
                    ELSE 3
                END,
                is_verified DESC,
                average_rating DESC,
                business_name
            LIMIT @PageSize OFFSET @Offset
            """,
            new
            {
                Category = NormalizeFilter(category),
                City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
                Latitude = latitude,
                Longitude = longitude,
                HasGeo = hasGeo,
                RadiusKm = radiusKm <= 0 ? 10 : radiusKm,
                PageSize = pageSize,
                Offset = offset
            });

        return rows.ToArray();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.ExecuteAsync("DELETE FROM professionals WHERE id = @Id AND user_id = @UserId", new { Id = id, UserId = userId });
        return rows > 0;
    }

    public async Task<ProfessionalServiceItem> AddServiceAsync(Guid professionalId, string serviceName, string? description, string? priceRange, int displayOrder, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<ProfessionalServiceItem>(
            """
            INSERT INTO professional_services (professional_id, service_name, description, price_range, display_order)
            VALUES (@ProfessionalId, @ServiceName, @Description, @PriceRange, @DisplayOrder)
            RETURNING
                id AS Id,
                professional_id AS ProfessionalId,
                service_name AS ServiceName,
                description AS Description,
                price_range AS PriceRange,
                display_order AS DisplayOrder,
                created_at AS CreatedAt
            """,
            new { ProfessionalId = professionalId, ServiceName = serviceName, Description = description, PriceRange = priceRange, DisplayOrder = displayOrder });
    }

    public async Task<IReadOnlyCollection<ProfessionalServiceItem>> GetServicesAsync(Guid professionalId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.QueryAsync<ProfessionalServiceItem>(
            """
            SELECT id AS Id, professional_id AS ProfessionalId, service_name AS ServiceName,
                   description AS Description, price_range AS PriceRange, display_order AS DisplayOrder,
                   created_at AS CreatedAt
            FROM professional_services
            WHERE professional_id = @ProfessionalId
            ORDER BY display_order, created_at
            """,
            new { ProfessionalId = professionalId });
        return rows.ToArray();
    }

    public async Task<ProfessionalPhoto> AddPhotoAsync(Guid professionalId, Guid mediaId, string? mediaUrl, int displayOrder, string? caption, bool isPrimary, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        if (isPrimary)
        {
            await cn.ExecuteAsync("UPDATE professional_photos SET is_primary = false WHERE professional_id = @ProfessionalId", new { ProfessionalId = professionalId }, tx);
        }

        var photo = await cn.QuerySingleAsync<ProfessionalPhoto>(
            """
            INSERT INTO professional_photos (professional_id, media_id, media_url, display_order, caption, is_primary)
            VALUES (@ProfessionalId, @MediaId, @MediaUrl, @DisplayOrder, @Caption, @IsPrimary)
            ON CONFLICT (professional_id, media_id)
            DO UPDATE SET media_url = EXCLUDED.media_url,
                          display_order = EXCLUDED.display_order,
                          caption = EXCLUDED.caption,
                          is_primary = EXCLUDED.is_primary
            RETURNING id AS Id, professional_id AS ProfessionalId, media_id AS MediaId, media_url AS MediaUrl,
                      display_order AS DisplayOrder, caption AS Caption, is_primary AS IsPrimary, created_at AS CreatedAt
            """,
            new { ProfessionalId = professionalId, MediaId = mediaId, MediaUrl = mediaUrl, DisplayOrder = displayOrder, Caption = caption, IsPrimary = isPrimary },
            tx);

        tx.Commit();
        return photo;
    }

    public async Task<IReadOnlyCollection<ProfessionalPhoto>> GetPhotosAsync(Guid professionalId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.QueryAsync<ProfessionalPhoto>(
            """
            SELECT id AS Id, professional_id AS ProfessionalId, media_id AS MediaId, media_url AS MediaUrl,
                   display_order AS DisplayOrder, caption AS Caption, is_primary AS IsPrimary, created_at AS CreatedAt
            FROM professional_photos
            WHERE professional_id = @ProfessionalId
            ORDER BY display_order, created_at
            """,
            new { ProfessionalId = professionalId });
        return rows.ToArray();
    }

    public async Task<bool> SetSubscriptionAsync(Guid professionalId, string plan, string status, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.ExecuteAsync(
            "UPDATE professionals SET subscription_plan = @Plan, subscription_status = @Status, updated_at = now() WHERE id = @ProfessionalId",
            new { ProfessionalId = professionalId, Plan = plan, Status = status });
        return rows > 0;
    }

    public async Task<bool> SetVerifiedAsync(Guid professionalId, bool isVerified, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.ExecuteAsync(
            "UPDATE professionals SET is_verified = @IsVerified, updated_at = now() WHERE id = @ProfessionalId",
            new { ProfessionalId = professionalId, IsVerified = isVerified });
        return rows > 0;
    }

    private async Task<Professional?> GetSingleAsync(string whereClause, object parameters)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<Professional>(
            $"""
            SELECT
                id AS Id,
                user_id AS UserId,
                business_name AS BusinessName,
                category AS Category,
                description AS Description,
                address AS Address,
                city AS City,
                postal_code AS PostalCode,
                latitude AS Latitude,
                longitude AS Longitude,
                phone AS Phone,
                email AS Email,
                website AS Website,
                subscription_plan AS SubscriptionPlan,
                subscription_status AS SubscriptionStatus,
                is_verified AS IsVerified,
                average_rating AS AverageRating,
                review_count AS ReviewCount,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM professionals
            WHERE {whereClause}
            """,
            parameters);
    }

    private static string? NormalizeFilter(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}

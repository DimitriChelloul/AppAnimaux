using Dapper;
using LocationService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace LocationService.DAL.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly IDbConnectionFactory _db;

    public LocationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<UserLocation?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<UserLocation>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                latitude AS Latitude,
                longitude AS Longitude,
                city AS City,
                postal_code AS PostalCode,
                country AS Country,
                is_active AS IsActive,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM user_locations
            WHERE user_id = @UserId
              AND is_active = true
            ORDER BY updated_at DESC
            LIMIT 1
            """,
            new { UserId = userId });
    }

    public async Task<UserLocation> UpsertUserLocationAsync(UserLocation location, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<UserLocation>(
            """
            WITH deactivated AS (
                UPDATE user_locations
                SET is_active = false,
                    updated_at = now()
                WHERE user_id = @UserId
                  AND is_active = true
                  AND id <> @Id
            )
            INSERT INTO user_locations (
                id, user_id, latitude, longitude, city, postal_code, country, is_active
            )
            VALUES (
                @Id, @UserId, @Latitude, @Longitude, @City, @PostalCode, @Country, true
            )
            ON CONFLICT (id)
            DO UPDATE SET
                latitude = EXCLUDED.latitude,
                longitude = EXCLUDED.longitude,
                city = EXCLUDED.city,
                postal_code = EXCLUDED.postal_code,
                country = EXCLUDED.country,
                is_active = true,
                updated_at = now()
            RETURNING
                id AS Id,
                user_id AS UserId,
                latitude AS Latitude,
                longitude AS Longitude,
                city AS City,
                postal_code AS PostalCode,
                country AS Country,
                is_active AS IsActive,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            location);
    }

    public async Task<IReadOnlyCollection<UserLocation>> GetActiveWithinBoundsAsync(decimal minLatitude, decimal maxLatitude, decimal minLongitude, decimal maxLongitude, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var locations = await cn.QueryAsync<UserLocation>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                latitude AS Latitude,
                longitude AS Longitude,
                city AS City,
                postal_code AS PostalCode,
                country AS Country,
                is_active AS IsActive,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM user_locations
            WHERE is_active = true
              AND latitude BETWEEN @MinLatitude AND @MaxLatitude
              AND longitude BETWEEN @MinLongitude AND @MaxLongitude
            """,
            new { MinLatitude = minLatitude, MaxLatitude = maxLatitude, MinLongitude = minLongitude, MaxLongitude = maxLongitude });

        return locations.ToArray();
    }

    public async Task<LocationPreference?> GetPreferenceAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<LocationPreference>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                search_radius_km AS SearchRadiusKm,
                allow_remote AS AllowRemote,
                notify_on_match AS NotifyOnMatch,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM location_preferences
            WHERE user_id = @UserId
            """,
            new { UserId = userId });
    }

    public async Task<LocationPreference> UpsertPreferenceAsync(LocationPreference preference, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<LocationPreference>(
            """
            INSERT INTO location_preferences (
                id, user_id, search_radius_km, allow_remote, notify_on_match
            )
            VALUES (
                @Id, @UserId, @SearchRadiusKm, @AllowRemote, @NotifyOnMatch
            )
            ON CONFLICT (user_id)
            DO UPDATE SET
                search_radius_km = EXCLUDED.search_radius_km,
                allow_remote = EXCLUDED.allow_remote,
                notify_on_match = EXCLUDED.notify_on_match,
                updated_at = now()
            RETURNING
                id AS Id,
                user_id AS UserId,
                search_radius_km AS SearchRadiusKm,
                allow_remote AS AllowRemote,
                notify_on_match AS NotifyOnMatch,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            preference);
    }
}

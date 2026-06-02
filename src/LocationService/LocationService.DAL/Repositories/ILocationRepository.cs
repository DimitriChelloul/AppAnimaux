using LocationService.Domain.Entities;

namespace LocationService.DAL.Repositories;

public interface ILocationRepository
{
    Task<UserLocation?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);
    Task<UserLocation> UpsertUserLocationAsync(UserLocation location, CancellationToken ct);
    Task<IReadOnlyCollection<UserLocation>> GetActiveWithinBoundsAsync(decimal minLatitude, decimal maxLatitude, decimal minLongitude, decimal maxLongitude, CancellationToken ct);
    Task<LocationPreference?> GetPreferenceAsync(Guid userId, CancellationToken ct);
    Task<LocationPreference> UpsertPreferenceAsync(LocationPreference preference, CancellationToken ct);
}

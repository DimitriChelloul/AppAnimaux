using LocationService.BLL.Models;

namespace LocationService.BLL.Services;

public interface ILocationAppService
{
    Task<UserLocationResponse?> GetMineAsync(Guid userId, CancellationToken ct);
    Task<UserLocationResponse> UpsertMineAsync(Guid userId, UpsertUserLocationRequest request, CancellationToken ct);
    Task<LocationPreferenceResponse?> GetPreferenceAsync(Guid userId, CancellationToken ct);
    Task<LocationPreferenceResponse> UpsertPreferenceAsync(Guid userId, UpsertLocationPreferenceRequest request, CancellationToken ct);
    DistanceResponse CalculateDistance(DistanceRequest request);
    Task<IReadOnlyCollection<NearbyLocationResponse>> FindNearbyAsync(NearbyLocationsRequest request, CancellationToken ct);
}

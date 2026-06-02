using LocationService.BLL.Models;
using LocationService.DAL.Repositories;
using LocationService.Domain.Entities;

namespace LocationService.BLL.Services;

public sealed class LocationAppService : ILocationAppService
{
    private const double EarthRadiusKm = 6371.0088;
    private readonly ILocationRepository _locations;

    public LocationAppService(ILocationRepository locations) => _locations = locations;

    public async Task<UserLocationResponse?> GetMineAsync(Guid userId, CancellationToken ct)
    {
        var location = await _locations.GetActiveByUserIdAsync(userId, ct);
        return location is null ? null : UserLocationResponse.From(location);
    }

    public async Task<UserLocationResponse> UpsertMineAsync(Guid userId, UpsertUserLocationRequest request, CancellationToken ct)
    {
        ValidateCoordinates(request.Latitude, request.Longitude);

        var existing = await _locations.GetActiveByUserIdAsync(userId, ct);
        var saved = await _locations.UpsertUserLocationAsync(
            new UserLocation
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                UserId = userId,
                Latitude = RoundCoordinate(request.Latitude),
                Longitude = RoundCoordinate(request.Longitude),
                City = NormalizeOptional(request.City),
                PostalCode = NormalizeOptional(request.PostalCode),
                Country = NormalizeOptional(request.Country),
                IsActive = true
            },
            ct);

        return UserLocationResponse.From(saved);
    }

    public async Task<LocationPreferenceResponse?> GetPreferenceAsync(Guid userId, CancellationToken ct)
    {
        var preference = await _locations.GetPreferenceAsync(userId, ct);
        return preference is null ? null : LocationPreferenceResponse.From(preference);
    }

    public async Task<LocationPreferenceResponse> UpsertPreferenceAsync(Guid userId, UpsertLocationPreferenceRequest request, CancellationToken ct)
    {
        ValidateRadius(request.SearchRadiusKm);

        var existing = await _locations.GetPreferenceAsync(userId, ct);
        var saved = await _locations.UpsertPreferenceAsync(
            new LocationPreference
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                UserId = userId,
                SearchRadiusKm = request.SearchRadiusKm,
                AllowRemote = request.AllowRemote,
                NotifyOnMatch = request.NotifyOnMatch
            },
            ct);

        return LocationPreferenceResponse.From(saved);
    }

    public DistanceResponse CalculateDistance(DistanceRequest request)
    {
        ValidateCoordinates(request.OriginLatitude, request.OriginLongitude);
        ValidateCoordinates(request.TargetLatitude, request.TargetLongitude);

        return new DistanceResponse(CalculateDistanceKm(
            request.OriginLatitude,
            request.OriginLongitude,
            request.TargetLatitude,
            request.TargetLongitude));
    }

    public async Task<IReadOnlyCollection<NearbyLocationResponse>> FindNearbyAsync(NearbyLocationsRequest request, CancellationToken ct)
    {
        ValidateCoordinates(request.Latitude, request.Longitude);
        ValidateRadius(request.RadiusKm);

        var latitudeDelta = (decimal)(request.RadiusKm / 111.32d);
        var longitudeDelta = (decimal)(request.RadiusKm / (111.32d * Math.Max(Math.Cos(ToRadians((double)request.Latitude)), 0.01d)));

        var candidates = await _locations.GetActiveWithinBoundsAsync(
            request.Latitude - latitudeDelta,
            request.Latitude + latitudeDelta,
            request.Longitude - longitudeDelta,
            request.Longitude + longitudeDelta,
            ct);

        return candidates
            .Select(location => new NearbyLocationResponse(
                UserLocationResponse.From(location),
                CalculateDistanceKm(request.Latitude, request.Longitude, location.Latitude, location.Longitude)))
            .Where(location => location.DistanceKm <= request.RadiusKm)
            .OrderBy(location => location.DistanceKm)
            .ToArray();
    }

    private static double CalculateDistanceKm(decimal originLatitude, decimal originLongitude, decimal targetLatitude, decimal targetLongitude)
    {
        var originLat = ToRadians((double)originLatitude);
        var targetLat = ToRadians((double)targetLatitude);
        var deltaLat = ToRadians((double)(targetLatitude - originLatitude));
        var deltaLon = ToRadians((double)(targetLongitude - originLongitude));

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2)
            + Math.Cos(originLat) * Math.Cos(targetLat) * Math.Pow(Math.Sin(deltaLon / 2), 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(EarthRadiusKm * c, 3);
    }

    private static void ValidateCoordinates(decimal latitude, decimal longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentException("Latitude must be between -90 and 90.");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentException("Longitude must be between -180 and 180.");
        }
    }

    private static void ValidateRadius(int radiusKm)
    {
        if (radiusKm is < 1 or > 500)
        {
            throw new ArgumentException("Radius must be between 1 and 500 kilometers.");
        }
    }

    private static decimal RoundCoordinate(decimal value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}

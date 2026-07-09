using LocationService.BLL.Models;
using LocationService.DAL.Repositories;
using LocationService.Domain.Entities;
using Shared.Geo;

namespace LocationService.BLL.Services;

public sealed class LocationAppService : ILocationAppService
{
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
        var origin = ToGeoPoint(request.OriginLatitude, request.OriginLongitude);
        var target = ToGeoPoint(request.TargetLatitude, request.TargetLongitude);

        return new DistanceResponse(Math.Round(GeoDistance.KilometersBetween(origin, target), 3));
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

        var origin = ToGeoPoint(request.Latitude, request.Longitude);
        return candidates
            .Select(location => new NearbyLocationResponse(
                UserLocationResponse.From(location),
                Math.Round(GeoDistance.KilometersBetween(origin, ToGeoPoint(location.Latitude, location.Longitude)), 3)))
            .Where(location => location.DistanceKm <= request.RadiusKm)
            .OrderBy(location => location.DistanceKm)
            .ToArray();
    }

    private static GeoPoint ToGeoPoint(decimal latitude, decimal longitude) => new((double)latitude, (double)longitude);

    private static void ValidateCoordinates(decimal latitude, decimal longitude) => _ = ToGeoPoint(latitude, longitude);

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
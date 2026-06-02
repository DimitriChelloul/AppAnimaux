using LocationService.Domain.Entities;

namespace LocationService.BLL.Models;

public sealed record UpsertUserLocationRequest(
    decimal Latitude,
    decimal Longitude,
    string? City,
    string? PostalCode,
    string? Country);

public sealed record UpsertLocationPreferenceRequest(
    int SearchRadiusKm,
    bool AllowRemote,
    bool NotifyOnMatch);

public sealed record DistanceRequest(
    decimal OriginLatitude,
    decimal OriginLongitude,
    decimal TargetLatitude,
    decimal TargetLongitude);

public sealed record NearbyLocationsRequest(
    decimal Latitude,
    decimal Longitude,
    int RadiusKm);

public sealed record UserLocationResponse(
    Guid Id,
    Guid UserId,
    decimal Latitude,
    decimal Longitude,
    string? City,
    string? PostalCode,
    string? Country,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static UserLocationResponse From(UserLocation location) =>
        new(
            location.Id,
            location.UserId,
            location.Latitude,
            location.Longitude,
            location.City,
            location.PostalCode,
            location.Country,
            location.IsActive,
            location.CreatedAt,
            location.UpdatedAt);
}

public sealed record LocationPreferenceResponse(
    Guid Id,
    Guid UserId,
    int SearchRadiusKm,
    bool AllowRemote,
    bool NotifyOnMatch,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static LocationPreferenceResponse From(LocationPreference preference) =>
        new(
            preference.Id,
            preference.UserId,
            preference.SearchRadiusKm,
            preference.AllowRemote,
            preference.NotifyOnMatch,
            preference.CreatedAt,
            preference.UpdatedAt);
}

public sealed record NearbyLocationResponse(
    UserLocationResponse Location,
    double DistanceKm);

public sealed record DistanceResponse(double DistanceKm);

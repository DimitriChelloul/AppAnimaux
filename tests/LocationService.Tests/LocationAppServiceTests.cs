using LocationService.BLL.Models;
using LocationService.BLL.Services;
using LocationService.DAL.Repositories;
using LocationService.Domain.Entities;

namespace LocationService.Tests;

public sealed class LocationAppServiceTests
{
    [Fact]
    public void CalculateDistance_returns_expected_distance_between_paris_and_lyon()
    {
        var service = new LocationAppService(new InMemoryLocationRepository());

        var result = service.CalculateDistance(new DistanceRequest(
            OriginLatitude: 48.8566m,
            OriginLongitude: 2.3522m,
            TargetLatitude: 45.7640m,
            TargetLongitude: 4.8357m));

        Assert.InRange(result.DistanceKm, 390, 395);
    }

    [Fact]
    public void CalculateDistance_rejects_invalid_coordinates()
    {
        var service = new LocationAppService(new InMemoryLocationRepository());

        Assert.Throws<ArgumentException>(() => service.CalculateDistance(new DistanceRequest(91m, 2m, 45m, 4m)));
        Assert.Throws<ArgumentException>(() => service.CalculateDistance(new DistanceRequest(48m, 181m, 45m, 4m)));
    }

    [Fact]
    public async Task UpsertMine_normalizes_and_reuses_active_location()
    {
        var userId = Guid.NewGuid();
        var repository = new InMemoryLocationRepository();
        var service = new LocationAppService(repository);

        var created = await service.UpsertMineAsync(
            userId,
            new UpsertUserLocationRequest(48.8566123m, 2.3522456m, " Paris ", " 75001 ", " FR "),
            CancellationToken.None);

        var updated = await service.UpsertMineAsync(
            userId,
            new UpsertUserLocationRequest(45.7640432m, 4.8356599m, "Lyon", null, "FR"),
            CancellationToken.None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(45.764043m, updated.Latitude);
        Assert.Equal(4.835660m, updated.Longitude);
        Assert.Equal("Lyon", updated.City);
        Assert.Null(updated.PostalCode);
        Assert.Equal("FR", updated.Country);
    }

    [Fact]
    public async Task UpsertPreference_validates_radius()
    {
        var service = new LocationAppService(new InMemoryLocationRepository());
        var userId = Guid.NewGuid();

        var preference = await service.UpsertPreferenceAsync(
            userId,
            new UpsertLocationPreferenceRequest(25, AllowRemote: true, NotifyOnMatch: false),
            CancellationToken.None);

        Assert.Equal(25, preference.SearchRadiusKm);
        Assert.True(preference.AllowRemote);
        Assert.False(preference.NotifyOnMatch);
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpsertPreferenceAsync(
            userId,
            new UpsertLocationPreferenceRequest(0, AllowRemote: false, NotifyOnMatch: true),
            CancellationToken.None));
    }

    [Fact]
    public async Task FindNearby_returns_only_locations_inside_radius_ordered_by_distance()
    {
        var repository = new InMemoryLocationRepository();
        var service = new LocationAppService(repository);

        await service.UpsertMineAsync(Guid.NewGuid(), new UpsertUserLocationRequest(48.8570m, 2.3520m, "Paris center", null, "FR"), CancellationToken.None);
        await service.UpsertMineAsync(Guid.NewGuid(), new UpsertUserLocationRequest(48.9020m, 2.3050m, "Clichy", null, "FR"), CancellationToken.None);
        await service.UpsertMineAsync(Guid.NewGuid(), new UpsertUserLocationRequest(45.7640m, 4.8357m, "Lyon", null, "FR"), CancellationToken.None);

        var nearby = await service.FindNearbyAsync(new NearbyLocationsRequest(48.8566m, 2.3522m, 10), CancellationToken.None);

        Assert.Equal(2, nearby.Count);
        Assert.Collection(
            nearby,
            first => Assert.Equal("Paris center", first.Location.City),
            second => Assert.Equal("Clichy", second.Location.City));
    }

    private sealed class InMemoryLocationRepository : ILocationRepository
    {
        private readonly List<UserLocation> _locations = [];
        private readonly List<LocationPreference> _preferences = [];

        public Task<UserLocation?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return Task.FromResult(_locations.SingleOrDefault(location => location.UserId == userId && location.IsActive));
        }

        public Task<UserLocation> UpsertUserLocationAsync(UserLocation location, CancellationToken ct)
        {
            _locations.RemoveAll(item => item.UserId == location.UserId && item.Id != location.Id);
            _locations.RemoveAll(item => item.Id == location.Id);

            var now = DateTimeOffset.UtcNow;
            var saved = new UserLocation
            {
                Id = location.Id,
                UserId = location.UserId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                City = location.City,
                PostalCode = location.PostalCode,
                Country = location.Country,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _locations.Add(saved);
            return Task.FromResult(saved);
        }

        public Task<IReadOnlyCollection<UserLocation>> GetActiveWithinBoundsAsync(decimal minLatitude, decimal maxLatitude, decimal minLongitude, decimal maxLongitude, CancellationToken ct)
        {
            IReadOnlyCollection<UserLocation> result = _locations
                .Where(location =>
                    location.IsActive &&
                    location.Latitude >= minLatitude &&
                    location.Latitude <= maxLatitude &&
                    location.Longitude >= minLongitude &&
                    location.Longitude <= maxLongitude)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<LocationPreference?> GetPreferenceAsync(Guid userId, CancellationToken ct)
        {
            return Task.FromResult(_preferences.SingleOrDefault(preference => preference.UserId == userId));
        }

        public Task<LocationPreference> UpsertPreferenceAsync(LocationPreference preference, CancellationToken ct)
        {
            _preferences.RemoveAll(item => item.UserId == preference.UserId);

            var now = DateTimeOffset.UtcNow;
            var saved = new LocationPreference
            {
                Id = preference.Id,
                UserId = preference.UserId,
                SearchRadiusKm = preference.SearchRadiusKm,
                AllowRemote = preference.AllowRemote,
                NotifyOnMatch = preference.NotifyOnMatch,
                CreatedAt = now,
                UpdatedAt = now
            };

            _preferences.Add(saved);
            return Task.FromResult(saved);
        }
    }
}

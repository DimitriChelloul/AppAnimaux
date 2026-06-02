namespace LocationService.Domain.Entities;

public sealed class LocationPreference
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int SearchRadiusKm { get; init; }
    public bool AllowRemote { get; init; }
    public bool NotifyOnMatch { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

namespace MediaService.Domain.Entities;

public sealed class MediaUsage
{
    public long Id { get; init; }
    public Guid MediaId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string UsageType { get; init; } = "attachment";
    public DateTimeOffset CreatedAt { get; init; }
}

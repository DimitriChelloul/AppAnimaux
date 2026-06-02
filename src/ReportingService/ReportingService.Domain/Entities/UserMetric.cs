namespace ReportingService.Domain.Entities;

public sealed class UserMetric
{
    public Guid UserId { get; init; }
    public string MetricKey { get; init; } = "";
    public long MetricValue { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

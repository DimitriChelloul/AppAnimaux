namespace ReportingService.Domain.Entities;

public sealed class DailyMetric
{
    public DateOnly Day { get; init; }
    public string MetricKey { get; init; } = "";
    public long MetricValue { get; init; }
    public string DimensionsJson { get; init; } = "{}";
    public DateTimeOffset UpdatedAt { get; init; }
}

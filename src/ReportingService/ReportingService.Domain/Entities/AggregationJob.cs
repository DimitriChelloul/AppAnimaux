namespace ReportingService.Domain.Entities;

public sealed class AggregationJob
{
    public long Id { get; init; }
    public string JobName { get; init; } = "";
    public string Status { get; init; } = "success";
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public Guid? LastProcessedEventId { get; init; }
    public string? Error { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

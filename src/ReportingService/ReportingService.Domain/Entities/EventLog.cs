namespace ReportingService.Domain.Entities;

public sealed class EventLog
{
    public long Id { get; init; }
    public Guid EventId { get; init; }
    public string EventType { get; init; } = "";
    public string SourceService { get; init; } = "";
    public string? AggregateType { get; init; }
    public Guid? AggregateId { get; init; }
    public Guid? ActorUserId { get; init; }
    public DateTimeOffset OccurredOn { get; init; }
    public DateTimeOffset ReceivedOn { get; init; }
    public string? PayloadJson { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string? MetaJson { get; init; }
}

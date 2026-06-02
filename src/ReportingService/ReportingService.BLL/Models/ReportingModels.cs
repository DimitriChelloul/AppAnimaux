using ReportingService.Domain.Entities;

namespace ReportingService.BLL.Models;

public sealed record AppendEventRequest(
    Guid? EventId,
    string EventType,
    string SourceService,
    string? AggregateType,
    Guid? AggregateId,
    Guid? ActorUserId,
    DateTimeOffset? OccurredOn,
    string? PayloadJson,
    Guid? CorrelationId,
    Guid? CausationId,
    string? MetaJson);

public sealed record EventSearchRequest(
    string? EventType,
    string? SourceService,
    Guid? ActorUserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 50);

public sealed record IncrementDailyMetricRequest(DateOnly? Day, string MetricKey, long Amount = 1, string? DimensionsJson = null);

public sealed record DailyMetricSearchRequest(DateOnly? From, DateOnly? To, string? MetricKey);

public sealed record IncrementUserMetricRequest(Guid UserId, string MetricKey, long Amount = 1);

public sealed record RecordAggregationJobRequest(
    string JobName,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    Guid? LastProcessedEventId,
    string? Error);

public sealed record AggregationJobSearchRequest(string? JobName, int Page = 1, int PageSize = 20);

public sealed record ReportingDashboardResponse(
    IReadOnlyCollection<EventLog> RecentEvents,
    IReadOnlyCollection<DailyMetric> DailyMetrics,
    IReadOnlyCollection<AggregationJob> RecentJobs);

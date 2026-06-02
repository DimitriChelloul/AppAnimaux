using ReportingService.Domain.Entities;

namespace ReportingService.DAL.Repositories;

public interface IReportingRepository
{
    Task<EventLog> AppendEventAsync(EventLog eventLog, CancellationToken ct);
    Task<IReadOnlyCollection<EventLog>> SearchEventsAsync(string? eventType, string? sourceService, Guid? actorUserId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct);
    Task<DailyMetric> IncrementDailyMetricAsync(DateOnly day, string metricKey, long amount, string dimensionsJson, CancellationToken ct);
    Task<IReadOnlyCollection<DailyMetric>> GetDailyMetricsAsync(DateOnly from, DateOnly to, string? metricKey, CancellationToken ct);
    Task<UserMetric> IncrementUserMetricAsync(Guid userId, string metricKey, long amount, CancellationToken ct);
    Task<IReadOnlyCollection<UserMetric>> GetUserMetricsAsync(Guid userId, CancellationToken ct);
    Task<AggregationJob> RecordAggregationJobAsync(AggregationJob job, CancellationToken ct);
    Task<IReadOnlyCollection<AggregationJob>> GetAggregationJobsAsync(string? jobName, int page, int pageSize, CancellationToken ct);
}

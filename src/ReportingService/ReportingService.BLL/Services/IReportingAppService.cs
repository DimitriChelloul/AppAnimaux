using ReportingService.BLL.Models;
using ReportingService.Domain.Entities;

namespace ReportingService.BLL.Services;

public interface IReportingAppService
{
    Task<EventLog> AppendEventAsync(AppendEventRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<EventLog>> SearchEventsAsync(EventSearchRequest request, CancellationToken ct);
    Task<DailyMetric> IncrementDailyMetricAsync(IncrementDailyMetricRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<DailyMetric>> GetDailyMetricsAsync(DailyMetricSearchRequest request, CancellationToken ct);
    Task<UserMetric> IncrementUserMetricAsync(IncrementUserMetricRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<UserMetric>> GetUserMetricsAsync(Guid userId, CancellationToken ct);
    Task<AggregationJob> RecordAggregationJobAsync(RecordAggregationJobRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<AggregationJob>> GetAggregationJobsAsync(AggregationJobSearchRequest request, CancellationToken ct);
    Task<ReportingDashboardResponse> GetDashboardAsync(DateOnly? from, DateOnly? to, CancellationToken ct);
}

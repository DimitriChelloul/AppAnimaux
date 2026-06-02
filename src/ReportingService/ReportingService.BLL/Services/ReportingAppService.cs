using ReportingService.BLL.Models;
using ReportingService.DAL.Repositories;
using ReportingService.Domain.Entities;

namespace ReportingService.BLL.Services;

public sealed class ReportingAppService : IReportingAppService
{
    private static readonly HashSet<string> ValidJobStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "running",
        "success",
        "failed"
    };

    private readonly IReportingRepository _reporting;

    public ReportingAppService(IReportingRepository reporting) => _reporting = reporting;

    public Task<EventLog> AppendEventAsync(AppendEventRequest request, CancellationToken ct)
    {
        var eventType = NormalizeRequired(request.EventType, "Event type is required.");
        var sourceService = NormalizeRequired(request.SourceService, "Source service is required.");
        var occurredOn = request.OccurredOn ?? DateTimeOffset.UtcNow;

        return _reporting.AppendEventAsync(
            new EventLog
            {
                EventId = request.EventId.GetValueOrDefault(Guid.NewGuid()),
                EventType = eventType,
                SourceService = sourceService,
                AggregateType = NormalizeOptional(request.AggregateType),
                AggregateId = request.AggregateId,
                ActorUserId = request.ActorUserId,
                OccurredOn = occurredOn,
                PayloadJson = NormalizeJson(request.PayloadJson),
                CorrelationId = request.CorrelationId,
                CausationId = request.CausationId,
                MetaJson = NormalizeJson(request.MetaJson)
            },
            ct);
    }

    public Task<IReadOnlyCollection<EventLog>> SearchEventsAsync(EventSearchRequest request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        if (request.From is not null && request.To is not null && request.From > request.To)
        {
            throw new ArgumentException("From date must be before to date.");
        }

        return _reporting.SearchEventsAsync(
            NormalizeOptional(request.EventType),
            NormalizeOptional(request.SourceService),
            request.ActorUserId,
            request.From,
            request.To,
            page,
            pageSize,
            ct);
    }

    public Task<DailyMetric> IncrementDailyMetricAsync(IncrementDailyMetricRequest request, CancellationToken ct)
    {
        var metricKey = NormalizeMetricKey(request.MetricKey);
        if (request.Amount == 0)
        {
            throw new ArgumentException("Metric increment amount must not be zero.");
        }

        return _reporting.IncrementDailyMetricAsync(
            request.Day ?? DateOnly.FromDateTime(DateTime.UtcNow),
            metricKey,
            request.Amount,
            NormalizeJson(request.DimensionsJson),
            ct);
    }

    public Task<IReadOnlyCollection<DailyMetric>> GetDailyMetricsAsync(DailyMetricSearchRequest request, CancellationToken ct)
    {
        var to = request.To ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.From ?? to.AddDays(-30);
        if (from > to)
        {
            throw new ArgumentException("From date must be before to date.");
        }

        return _reporting.GetDailyMetricsAsync(from, to, NormalizeOptional(request.MetricKey), ct);
    }

    public Task<UserMetric> IncrementUserMetricAsync(IncrementUserMetricRequest request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.");
        }

        if (request.Amount == 0)
        {
            throw new ArgumentException("Metric increment amount must not be zero.");
        }

        return _reporting.IncrementUserMetricAsync(request.UserId, NormalizeMetricKey(request.MetricKey), request.Amount, ct);
    }

    public Task<IReadOnlyCollection<UserMetric>> GetUserMetricsAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.");
        }

        return _reporting.GetUserMetricsAsync(userId, ct);
    }

    public Task<AggregationJob> RecordAggregationJobAsync(RecordAggregationJobRequest request, CancellationToken ct)
    {
        var status = NormalizeRequired(request.Status, "Job status is required.").ToLowerInvariant();
        if (!ValidJobStatuses.Contains(status))
        {
            throw new ArgumentException("Invalid aggregation job status.");
        }

        if (request.StartedAt is not null && request.FinishedAt is not null && request.StartedAt > request.FinishedAt)
        {
            throw new ArgumentException("Job start date must be before finish date.");
        }

        return _reporting.RecordAggregationJobAsync(
            new AggregationJob
            {
                JobName = NormalizeRequired(request.JobName, "Job name is required."),
                Status = status,
                StartedAt = request.StartedAt,
                FinishedAt = request.FinishedAt,
                LastProcessedEventId = request.LastProcessedEventId,
                Error = NormalizeOptional(request.Error)
            },
            ct);
    }

    public Task<IReadOnlyCollection<AggregationJob>> GetAggregationJobsAsync(AggregationJobSearchRequest request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        return _reporting.GetAggregationJobsAsync(NormalizeOptional(request.JobName), page, pageSize, ct);
    }

    public async Task<ReportingDashboardResponse> GetDashboardAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var events = await SearchEventsAsync(new EventSearchRequest(null, null, null, null, null, 1, 20), ct);
        var metrics = await GetDailyMetricsAsync(new DailyMetricSearchRequest(from, to, null), ct);
        var jobs = await GetAggregationJobsAsync(new AggregationJobSearchRequest(null, 1, 10), ct);
        return new ReportingDashboardResponse(events, metrics, jobs);
    }

    private static string NormalizeMetricKey(string value)
    {
        var normalized = NormalizeRequired(value, "Metric key is required.").ToLowerInvariant();
        return normalized.Length > 100 ? throw new ArgumentException("Metric key is too long.") : normalized;
    }

    private static string NormalizeRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeJson(string? value) => string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
}

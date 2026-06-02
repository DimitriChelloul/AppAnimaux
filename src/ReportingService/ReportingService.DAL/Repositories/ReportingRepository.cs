using Dapper;
using ReportingService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace ReportingService.DAL.Repositories;

public sealed class ReportingRepository : IReportingRepository
{
    private readonly IDbConnectionFactory _db;

    public ReportingRepository(IDbConnectionFactory db) => _db = db;

    public async Task<EventLog> AppendEventAsync(EventLog eventLog, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<EventLog>(
            """
            INSERT INTO event_logs (
                event_id, event_type, source_service, aggregate_type, aggregate_id,
                actor_user_id, occurred_on, payload, correlation_id, causation_id, meta
            )
            VALUES (
                @EventId, @EventType, @SourceService, @AggregateType, @AggregateId,
                @ActorUserId, @OccurredOn, CAST(@PayloadJson AS jsonb),
                @CorrelationId, @CausationId, CAST(@MetaJson AS jsonb)
            )
            ON CONFLICT (event_id) DO UPDATE SET event_id = EXCLUDED.event_id
            RETURNING
                id AS Id, event_id AS EventId, event_type AS EventType, source_service AS SourceService,
                aggregate_type AS AggregateType, aggregate_id AS AggregateId, actor_user_id AS ActorUserId,
                occurred_on AS OccurredOn, received_on AS ReceivedOn, payload::text AS PayloadJson,
                correlation_id AS CorrelationId, causation_id AS CausationId, meta::text AS MetaJson
            """,
            eventLog);
    }

    public async Task<IReadOnlyCollection<EventLog>> SearchEventsAsync(string? eventType, string? sourceService, Guid? actorUserId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<EventLog>(
            """
            SELECT
                id AS Id, event_id AS EventId, event_type AS EventType, source_service AS SourceService,
                aggregate_type AS AggregateType, aggregate_id AS AggregateId, actor_user_id AS ActorUserId,
                occurred_on AS OccurredOn, received_on AS ReceivedOn, payload::text AS PayloadJson,
                correlation_id AS CorrelationId, causation_id AS CausationId, meta::text AS MetaJson
            FROM event_logs
            WHERE (@EventType IS NULL OR event_type = @EventType)
              AND (@SourceService IS NULL OR source_service = @SourceService)
              AND (@ActorUserId IS NULL OR actor_user_id = @ActorUserId)
              AND (@From IS NULL OR occurred_on >= @From)
              AND (@To IS NULL OR occurred_on <= @To)
            ORDER BY occurred_on DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { EventType = eventType, SourceService = sourceService, ActorUserId = actorUserId, From = from, To = to, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task<DailyMetric> IncrementDailyMetricAsync(DateOnly day, string metricKey, long amount, string dimensionsJson, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<DailyMetric>(
            """
            INSERT INTO daily_metrics (day, metric_key, metric_value, dimensions)
            VALUES (@Day, @MetricKey, @Amount, CAST(@DimensionsJson AS jsonb))
            ON CONFLICT (day, metric_key, dimensions)
            DO UPDATE SET metric_value = daily_metrics.metric_value + EXCLUDED.metric_value,
                          updated_at = now()
            RETURNING day AS Day, metric_key AS MetricKey, metric_value AS MetricValue,
                      dimensions::text AS DimensionsJson, updated_at AS UpdatedAt
            """,
            new { Day = day, MetricKey = metricKey, Amount = amount, DimensionsJson = dimensionsJson });
    }

    public async Task<IReadOnlyCollection<DailyMetric>> GetDailyMetricsAsync(DateOnly from, DateOnly to, string? metricKey, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<DailyMetric>(
            """
            SELECT day AS Day, metric_key AS MetricKey, metric_value AS MetricValue,
                   dimensions::text AS DimensionsJson, updated_at AS UpdatedAt
            FROM daily_metrics
            WHERE day >= @From AND day <= @To
              AND (@MetricKey IS NULL OR metric_key = @MetricKey)
            ORDER BY day DESC, metric_key
            """,
            new { From = from, To = to, MetricKey = metricKey });

        return rows.ToArray();
    }

    public async Task<UserMetric> IncrementUserMetricAsync(Guid userId, string metricKey, long amount, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<UserMetric>(
            """
            INSERT INTO user_metrics (user_id, metric_key, metric_value)
            VALUES (@UserId, @MetricKey, @Amount)
            ON CONFLICT (user_id, metric_key)
            DO UPDATE SET metric_value = user_metrics.metric_value + EXCLUDED.metric_value,
                          updated_at = now()
            RETURNING user_id AS UserId, metric_key AS MetricKey, metric_value AS MetricValue, updated_at AS UpdatedAt
            """,
            new { UserId = userId, MetricKey = metricKey, Amount = amount });
    }

    public async Task<IReadOnlyCollection<UserMetric>> GetUserMetricsAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<UserMetric>(
            """
            SELECT user_id AS UserId, metric_key AS MetricKey, metric_value AS MetricValue, updated_at AS UpdatedAt
            FROM user_metrics
            WHERE user_id = @UserId
            ORDER BY metric_key
            """,
            new { UserId = userId });

        return rows.ToArray();
    }

    public async Task<AggregationJob> RecordAggregationJobAsync(AggregationJob job, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<AggregationJob>(
            """
            INSERT INTO aggregation_jobs (job_name, status, started_at, finished_at, last_processed_event_id, error)
            VALUES (@JobName, @Status, @StartedAt, @FinishedAt, @LastProcessedEventId, @Error)
            RETURNING id AS Id, job_name AS JobName, status AS Status, started_at AS StartedAt,
                      finished_at AS FinishedAt, last_processed_event_id AS LastProcessedEventId,
                      error AS Error, created_at AS CreatedAt
            """,
            job);
    }

    public async Task<IReadOnlyCollection<AggregationJob>> GetAggregationJobsAsync(string? jobName, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<AggregationJob>(
            """
            SELECT id AS Id, job_name AS JobName, status AS Status, started_at AS StartedAt,
                   finished_at AS FinishedAt, last_processed_event_id AS LastProcessedEventId,
                   error AS Error, created_at AS CreatedAt
            FROM aggregation_jobs
            WHERE (@JobName IS NULL OR job_name = @JobName)
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { JobName = jobName, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }
}

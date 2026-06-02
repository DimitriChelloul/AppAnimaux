using ReportingService.BLL.Models;
using ReportingService.BLL.Services;
using ReportingService.DAL.Repositories;
using ReportingService.Domain.Entities;

namespace ReportingService.Tests;

public sealed class ReportingAppServiceTests
{
    [Fact]
    public async Task AppendEvent_requires_type_and_source()
    {
        var service = new ReportingAppService(new FakeReportingRepository());

        var request = new AppendEventRequest(null, "", "ForumService", null, null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AppendEventAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task AppendEvent_generates_event_id_and_defaults_json()
    {
        var service = new ReportingAppService(new FakeReportingRepository());

        var log = await service.AppendEventAsync(
            new AppendEventRequest(null, "Forum.TopicCreated", "ForumService", "topic", Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, log.EventId);
        Assert.Equal("{}", log.PayloadJson);
        Assert.Equal("{}", log.MetaJson);
    }

    [Fact]
    public async Task SearchEvents_rejects_invalid_date_range()
    {
        var service = new ReportingAppService(new FakeReportingRepository());

        var request = new EventSearchRequest(null, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1));

        await Assert.ThrowsAsync<ArgumentException>(() => service.SearchEventsAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task IncrementDailyMetric_accumulates_values()
    {
        var service = new ReportingAppService(new FakeReportingRepository());
        var day = new DateOnly(2026, 6, 2);

        await service.IncrementDailyMetricAsync(new IncrementDailyMetricRequest(day, "Events.Received", 2, null), CancellationToken.None);
        var metric = await service.IncrementDailyMetricAsync(new IncrementDailyMetricRequest(day, "events.received", 3, null), CancellationToken.None);

        Assert.Equal("events.received", metric.MetricKey);
        Assert.Equal(5, metric.MetricValue);
    }

    [Fact]
    public async Task IncrementUserMetric_requires_user_id()
    {
        var service = new ReportingAppService(new FakeReportingRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.IncrementUserMetricAsync(new IncrementUserMetricRequest(Guid.Empty, "messages.sent"), CancellationToken.None));
    }

    [Fact]
    public async Task RecordAggregationJob_rejects_invalid_status()
    {
        var service = new ReportingAppService(new FakeReportingRepository());

        var request = new RecordAggregationJobRequest("daily", "paused", null, null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordAggregationJobAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Dashboard_returns_recent_sections()
    {
        var repository = new FakeReportingRepository();
        var service = new ReportingAppService(repository);

        await service.AppendEventAsync(new AppendEventRequest(null, "Review.Created", "ReviewService", null, null, null, null, null, null, null, null), CancellationToken.None);
        await service.IncrementDailyMetricAsync(new IncrementDailyMetricRequest(null, "reviews.created"), CancellationToken.None);
        await service.RecordAggregationJobAsync(new RecordAggregationJobRequest("daily", "success", null, null, null, null), CancellationToken.None);

        var dashboard = await service.GetDashboardAsync(null, null, CancellationToken.None);

        Assert.Single(dashboard.RecentEvents);
        Assert.Single(dashboard.DailyMetrics);
        Assert.Single(dashboard.RecentJobs);
    }

    private sealed class FakeReportingRepository : IReportingRepository
    {
        private readonly List<EventLog> _events = [];
        private readonly List<DailyMetric> _dailyMetrics = [];
        private readonly List<UserMetric> _userMetrics = [];
        private readonly List<AggregationJob> _jobs = [];
        private long _nextEventId = 1;
        private long _nextJobId = 1;

        public Task<EventLog> AppendEventAsync(EventLog eventLog, CancellationToken ct)
        {
            var existing = _events.SingleOrDefault(x => x.EventId == eventLog.EventId);
            if (existing is not null)
            {
                return Task.FromResult(existing);
            }

            var created = new EventLog
            {
                Id = _nextEventId++,
                EventId = eventLog.EventId,
                EventType = eventLog.EventType,
                SourceService = eventLog.SourceService,
                AggregateType = eventLog.AggregateType,
                AggregateId = eventLog.AggregateId,
                ActorUserId = eventLog.ActorUserId,
                OccurredOn = eventLog.OccurredOn,
                ReceivedOn = DateTimeOffset.UtcNow,
                PayloadJson = eventLog.PayloadJson,
                CorrelationId = eventLog.CorrelationId,
                CausationId = eventLog.CausationId,
                MetaJson = eventLog.MetaJson
            };
            _events.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyCollection<EventLog>> SearchEventsAsync(string? eventType, string? sourceService, Guid? actorUserId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct)
        {
            var events = _events
                .Where(x => eventType is null || x.EventType == eventType)
                .Where(x => sourceService is null || x.SourceService == sourceService)
                .Where(x => actorUserId is null || x.ActorUserId == actorUserId)
                .Where(x => from is null || x.OccurredOn >= from)
                .Where(x => to is null || x.OccurredOn <= to)
                .OrderByDescending(x => x.OccurredOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<EventLog>>(events);
        }

        public Task<DailyMetric> IncrementDailyMetricAsync(DateOnly day, string metricKey, long amount, string dimensionsJson, CancellationToken ct)
        {
            var index = _dailyMetrics.FindIndex(x => x.Day == day && x.MetricKey == metricKey && x.DimensionsJson == dimensionsJson);
            if (index < 0)
            {
                var created = new DailyMetric { Day = day, MetricKey = metricKey, MetricValue = amount, DimensionsJson = dimensionsJson, UpdatedAt = DateTimeOffset.UtcNow };
                _dailyMetrics.Add(created);
                return Task.FromResult(created);
            }

            var current = _dailyMetrics[index];
            var updated = new DailyMetric { Day = current.Day, MetricKey = current.MetricKey, MetricValue = current.MetricValue + amount, DimensionsJson = current.DimensionsJson, UpdatedAt = DateTimeOffset.UtcNow };
            _dailyMetrics[index] = updated;
            return Task.FromResult(updated);
        }

        public Task<IReadOnlyCollection<DailyMetric>> GetDailyMetricsAsync(DateOnly from, DateOnly to, string? metricKey, CancellationToken ct)
        {
            var metrics = _dailyMetrics
                .Where(x => x.Day >= from && x.Day <= to)
                .Where(x => metricKey is null || x.MetricKey == metricKey)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<DailyMetric>>(metrics);
        }

        public Task<UserMetric> IncrementUserMetricAsync(Guid userId, string metricKey, long amount, CancellationToken ct)
        {
            var index = _userMetrics.FindIndex(x => x.UserId == userId && x.MetricKey == metricKey);
            if (index < 0)
            {
                var created = new UserMetric { UserId = userId, MetricKey = metricKey, MetricValue = amount, UpdatedAt = DateTimeOffset.UtcNow };
                _userMetrics.Add(created);
                return Task.FromResult(created);
            }

            var current = _userMetrics[index];
            var updated = new UserMetric { UserId = current.UserId, MetricKey = current.MetricKey, MetricValue = current.MetricValue + amount, UpdatedAt = DateTimeOffset.UtcNow };
            _userMetrics[index] = updated;
            return Task.FromResult(updated);
        }

        public Task<IReadOnlyCollection<UserMetric>> GetUserMetricsAsync(Guid userId, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyCollection<UserMetric>>(_userMetrics.Where(x => x.UserId == userId).ToArray());
        }

        public Task<AggregationJob> RecordAggregationJobAsync(AggregationJob job, CancellationToken ct)
        {
            var created = new AggregationJob
            {
                Id = _nextJobId++,
                JobName = job.JobName,
                Status = job.Status,
                StartedAt = job.StartedAt,
                FinishedAt = job.FinishedAt,
                LastProcessedEventId = job.LastProcessedEventId,
                Error = job.Error,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _jobs.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyCollection<AggregationJob>> GetAggregationJobsAsync(string? jobName, int page, int pageSize, CancellationToken ct)
        {
            var jobs = _jobs
                .Where(x => jobName is null || x.JobName == jobName)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<AggregationJob>>(jobs);
        }
    }
}

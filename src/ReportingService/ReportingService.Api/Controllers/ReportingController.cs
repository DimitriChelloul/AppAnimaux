using Microsoft.AspNetCore.Mvc;
using ReportingService.BLL.Models;
using ReportingService.BLL.Services;

namespace ReportingService.Api.Controllers;

[ApiController]
[Route("reporting")]
public sealed class ReportingController : ControllerBase
{
    private readonly IReportingAppService _reporting;

    public ReportingController(IReportingAppService reporting) => _reporting = reporting;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        try
        {
            return Ok(await _reporting.GetDashboardAsync(from, to, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("events")]
    public async Task<IActionResult> AppendEvent([FromBody] AppendEventRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _reporting.AppendEventAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("events")]
    public async Task<IActionResult> SearchEvents(
        [FromQuery] string? eventType,
        [FromQuery] string? sourceService,
        [FromQuery] Guid? actorUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await _reporting.SearchEventsAsync(new EventSearchRequest(eventType, sourceService, actorUserId, from, to, page, pageSize), ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("metrics/daily/increment")]
    public async Task<IActionResult> IncrementDailyMetric([FromBody] IncrementDailyMetricRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _reporting.IncrementDailyMetricAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("metrics/daily")]
    public async Task<IActionResult> GetDailyMetrics([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? metricKey, CancellationToken ct)
    {
        try
        {
            return Ok(await _reporting.GetDailyMetricsAsync(new DailyMetricSearchRequest(from, to, metricKey), ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("metrics/users/increment")]
    public async Task<IActionResult> IncrementUserMetric([FromBody] IncrementUserMetricRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _reporting.IncrementUserMetricAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("metrics/users/{userId:guid}")]
    public async Task<IActionResult> GetUserMetrics(Guid userId, CancellationToken ct)
    {
        try
        {
            return Ok(await _reporting.GetUserMetricsAsync(userId, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("aggregation-jobs")]
    public async Task<IActionResult> RecordAggregationJob([FromBody] RecordAggregationJobRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _reporting.RecordAggregationJobAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("aggregation-jobs")]
    public async Task<IActionResult> GetAggregationJobs([FromQuery] string? jobName, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _reporting.GetAggregationJobsAsync(new AggregationJobSearchRequest(jobName, page, pageSize), ct));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }
}

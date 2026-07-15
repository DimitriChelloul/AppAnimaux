using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Messaging.RabbitMq;
using Shared.Persistence.Abstractions;

namespace Shared.Messaging.Outbox;

public sealed record MessagingMaintenanceSnapshot(
    long PendingCount,
    long FailedCount,
    DateTime? OldestPendingOn,
    int DeletedProcessed,
    int DeletedFailed,
    int DeletedInbox);

public sealed class MessagingMaintenance
{
    private readonly IDbConnectionFactory _db;
    private readonly RabbitMqOptions _options;

    public MessagingMaintenance(IDbConnectionFactory db, IOptions<RabbitMqOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<MessagingMaintenanceSnapshot> RunOnceAsync(CancellationToken ct = default)
    {
        using var connection = _db.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var deletedProcessed = await connection.ExecuteAsync(
            "DELETE FROM outbox_messages WHERE status = 'processed' AND processed_on < now() - (@Days * interval '1 day')",
            new { Days = _options.ProcessedOutboxRetentionDays }, transaction);
        var deletedFailed = await connection.ExecuteAsync(
            "DELETE FROM outbox_messages WHERE status = 'failed' AND occurred_on < now() - (@Days * interval '1 day')",
            new { Days = _options.FailedOutboxRetentionDays }, transaction);

        var inboxExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT to_regclass('public.inbox_messages') IS NOT NULL", transaction: transaction);
        var deletedInbox = inboxExists
            ? await connection.ExecuteAsync(
                "DELETE FROM inbox_messages WHERE processed_on < now() - (@Days * interval '1 day')",
                new { Days = _options.InboxRetentionDays }, transaction)
            : 0;

        var snapshot = await connection.QuerySingleAsync<MessagingMaintenanceSnapshot>(
            """
            SELECT
                count(*) FILTER (WHERE status = 'pending') AS PendingCount,
                count(*) FILTER (WHERE status = 'failed') AS FailedCount,
                min(occurred_on) FILTER (WHERE status = 'pending') AS OldestPendingOn,
                @DeletedProcessed AS DeletedProcessed,
                @DeletedFailed AS DeletedFailed,
                @DeletedInbox AS DeletedInbox
            FROM outbox_messages
            """,
            new { DeletedProcessed = deletedProcessed, DeletedFailed = deletedFailed, DeletedInbox = deletedInbox },
            transaction);

        transaction.Commit();
        return snapshot;
    }
}

public sealed class MessagingMaintenanceHostedService : BackgroundService
{
    private readonly MessagingMaintenance _maintenance;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<MessagingMaintenanceHostedService> _logger;

    public MessagingMaintenanceHostedService(
        MessagingMaintenance maintenance,
        IOptions<RabbitMqOptions> options,
        ILogger<MessagingMaintenanceHostedService> logger)
    {
        _maintenance = maintenance;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.MaintenanceIntervalMinutes));
        do
        {
            try
            {
                var snapshot = await _maintenance.RunOnceAsync(stoppingToken);
                _logger.LogInformation(
                    "Messaging maintenance: Pending={PendingCount}, Failed={FailedCount}, OldestPendingOn={OldestPendingOn}, DeletedProcessed={DeletedProcessed}, DeletedFailed={DeletedFailed}, DeletedInbox={DeletedInbox}",
                    snapshot.PendingCount, snapshot.FailedCount, snapshot.OldestPendingOn,
                    snapshot.DeletedProcessed, snapshot.DeletedFailed, snapshot.DeletedInbox);

                if (snapshot.FailedCount > 0)
                {
                    _logger.LogWarning("Outbox contains {FailedCount} failed messages requiring operator action", snapshot.FailedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Messaging maintenance failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

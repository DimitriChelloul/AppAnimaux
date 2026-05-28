using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using System.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Messaging.Abstractions;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Routing;
using Shared.Persistence.Abstractions;

namespace Shared.Messaging.Outbox;



public sealed class OutboxPublisherHostedService : BackgroundService
{
    private readonly IEventPublisher _publisher;
    private readonly IEventRoutingMapper _routing;
    private readonly RabbitMqOptions _mq;
    private readonly ILogger<OutboxPublisherHostedService> _log;
    private readonly IDbConnectionFactory _db;

    public OutboxPublisherHostedService(
        IDbConnectionFactory db,
        IEventPublisher publisher,
        IEventRoutingMapper routing,
        IOptions<RabbitMqOptions> mq,
        ILogger<OutboxPublisherHostedService> log)
    {
        _publisher = publisher;
        _routing = routing;
        _mq = mq.Value;
        _log = log;
        _db = db;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await PublishBatchAsync(stoppingToken);

                // Si rien à publier, petite pause
                if (published == 0)
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogError(ex, "Outbox publisher loop error");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task<int> PublishBatchAsync(CancellationToken ct)
    {
        using var cn = _db.Create();
        if (cn.State != ConnectionState.Open) cn.Open();

        using var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted);

        // ⚠️ Adapte la table/colonnes à TON schéma outbox.
        // Ici : status=pending, retry_count, payload json, type, processed_on, error
        var rows = (await cn.QueryAsync<OutboxRow>(
            """
            SELECT id              AS Id,
                      message_id      AS MessageId,
                      aggregate_type  AS AggregateType,
                      aggregate_id    AS AggregateId,
                      type            AS Type,
                      payload::text   AS Payload,
                      occurred_on     AS OccurredOn,
                      status          AS Status,
                      processed_on    AS ProcessedOn,
                      error           AS Error
               FROM outbox_messages
               WHERE status = 'pending'
               ORDER BY occurred_on
               FOR UPDATE SKIP LOCKED
               LIMIT 20
            """,
            transaction: tx)).ToList();

        if (rows.Count == 0)
        {
            tx.Commit();
            return 0;
        }

        foreach (var r in rows)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var routingKey = _routing.GetRoutingKey(r.Type);

                // Payload déjà JSON. Si tu enveloppes, payload contient l'enveloppe.
                var body = Encoding.UTF8.GetBytes(r.Payload);

                await _publisher.PublishAsync(
                    exchange: _mq.ExchangeName,
                    routingKey: routingKey,
                    body: body,
                    headers: new Dictionary<string, object>
                    {
                        ["event_type"] = r.Type,
                        ["message_id"] = r.MessageId.ToString()
                    },
                    ct: ct
                );

                await cn.ExecuteAsync(
                    """
                    UPDATE outbox_messages
                    SET status = 'processed',
                        processed_on = now(),
                        error = NULL
                    WHERE id = @Id
                    """,
                    new { r.Id },
                    transaction: tx
                );
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed publishing outbox id={Id}, type={Type}", r.Id, r.Type);

                await cn.ExecuteAsync(
                    """
                    UPDATE outbox_messages
                       SET status = 'failed',
                           error = @Err
                       WHERE id = @Id
                    """,
                    new { Id = r.Id, Err = ex.Message },
                    transaction: tx
                );
            }
        }

        tx.Commit();
        return rows.Count;
    }
}


using Dapper;
using Shared.Persistence.Abstractions;

namespace Shared.Messaging.Outbox;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly IDbConnectionFactory _db;

    public OutboxRepository(IDbConnectionFactory db) => _db = db;

    public Task AddAsync(
        Guid messageId,
        string eventType,
        string payloadJson,
        string? aggregateType,
        Guid? aggregateId,
        CancellationToken ct = default)
        => AddAsync(
            messageId,
            eventType,
            payloadJson,
            aggregateType,
            aggregateId,
            DateTimeOffset.UtcNow,
            ct);

    public async Task AddAsync(
        Guid messageId,
        string eventType,
        string payloadJson,
        string? aggregateType,
        Guid? aggregateId,
        DateTimeOffset occurredOn,
        CancellationToken ct = default)
    {
        using var cn = _db.Create();
        cn.Open();
        await cn.ExecuteAsync(
            """
            INSERT INTO outbox_messages(
                message_id, aggregate_type, aggregate_id, type, payload, occurred_on, status)
            VALUES (
                @MessageId, @AggregateType, @AggregateId, @EventType,
                CAST(@Payload AS jsonb), @OccurredOn, 'pending')
            ON CONFLICT (message_id) DO NOTHING
            """,
            new
            {
                MessageId = messageId,
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                EventType = eventType,
                Payload = payloadJson,
                OccurredOn = occurredOn
            });
    }
}

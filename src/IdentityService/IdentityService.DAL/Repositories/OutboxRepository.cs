using Dapper;
using Shared.Persistence.Abstractions;

namespace IdentityService.DAL.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly IDbConnectionFactory _db;

    public OutboxRepository(IDbConnectionFactory db) => _db = db;

    public async Task AddAsync(Guid messageId, string type, string payloadJson, string? aggregateType, Guid? aggregateId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            INSERT INTO outbox_messages(message_id, aggregate_type, aggregate_id, type, payload, occurred_on, status)
            VALUES (@MessageId, @AggregateType, @AggregateId, @Type, CAST(@Payload AS jsonb), now(), 'pending')
            ON CONFLICT (message_id) DO NOTHING
            """,
            new
            {
                MessageId = messageId,
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                Type = type,
                Payload = payloadJson
            });
    }
}

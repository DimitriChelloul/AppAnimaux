namespace Shared.Messaging.Outbox;

public interface IOutboxRepository
{
    Task AddAsync(
        Guid messageId,
        string eventType,
        string payloadJson,
        string? aggregateType,
        Guid? aggregateId,
        CancellationToken ct = default);

    Task AddAsync(
        Guid messageId,
        string eventType,
        string payloadJson,
        string? aggregateType,
        Guid? aggregateId,
        DateTimeOffset occurredOn,
        CancellationToken ct = default);
}

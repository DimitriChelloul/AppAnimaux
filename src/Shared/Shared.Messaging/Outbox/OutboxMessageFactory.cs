using System.Text.Json;

namespace Shared.Messaging.Outbox;

public sealed record PendingOutboxMessage(
    Guid MessageId,
    string EventType,
    string Payload,
    DateTimeOffset OccurredOn);

public static class OutboxMessageFactory
{
    public static PendingOutboxMessage Create(string serviceName, string eventName, object data)
    {
        var messageId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;
        var eventType = $"{serviceName}.{eventName}";
        var payload = JsonSerializer.Serialize(new
        {
            type = eventType,
            version = 1,
            data,
            occurredOn,
            messageId
        });

        return new PendingOutboxMessage(messageId, eventType, payload, occurredOn);
    }
}

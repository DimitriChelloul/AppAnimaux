namespace Shared.Messaging.RabbitMq;

using RabbitMQ.Client;
using Shared.Messaging.Abstractions;

public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IRabbitMqConnection _conn;

    public RabbitMqEventPublisher(IRabbitMqConnection conn) => _conn = conn;

    public async Task PublishAsync(
        string exchange,
        string routingKey,
        ReadOnlyMemory<byte> body,
        IDictionary<string, object>? headers = null,
        CancellationToken ct = default)
    {
        var connection = await _conn.GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            ct);

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Headers = headers is null ? null : headers.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}

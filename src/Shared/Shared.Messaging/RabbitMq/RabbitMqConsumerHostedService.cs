namespace Shared.Messaging.RabbitMq;

using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events.Abstractions;
using Shared.Messaging.Consuming;
using Shared.Messaging.Serialization;

public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly IRabbitMqConnection _conn;
    private readonly RabbitMqOptions _opt;
    private readonly IEventHandlerRegistry _registry;
    private readonly ILogger<RabbitMqConsumerHostedService> _log;

    private IChannel? _channel;
    private string? _consumerTag;

    public RabbitMqConsumerHostedService(
        IRabbitMqConnection conn,
        IOptions<RabbitMqOptions> opt,
        IEventHandlerRegistry registry,
        ILogger<RabbitMqConsumerHostedService> log)
    {
        _conn = conn;
        _opt = opt.Value;
        _registry = registry;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _conn.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: _opt.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _opt.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        foreach (var binding in _opt.Bindings ?? Array.Empty<string>())
        {
            await _channel.QueueBindAsync(_opt.QueueName, _opt.ExchangeName, binding, arguments: null, cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _opt.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);

                var env = System.Text.Json.JsonSerializer.Deserialize<EventEnvelope<MinimalEvent>>(json, JsonDefaults.Options)
                          ?? throw new InvalidOperationException("Envelope is null");

                if (!_registry.TryGet(env.Type, out var handler))
                    throw new InvalidOperationException($"No handler for event type '{env.Type}'");

                await handler(json, stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Consumer failed. NACK requeue=false");
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _opt.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null && _consumerTag is not null)
            {
                // Correction : ajout de l'argument booléen noWait, suivi du CancellationToken
                await _channel.BasicCancelAsync(_consumerTag, false, cancellationToken);
                await _channel.DisposeAsync();
                _channel = null;
            }
        }
        catch { /* ignore */ }

        await base.StopAsync(cancellationToken);
    }

    private sealed record MinimalEvent : IntegrationEvent;
}

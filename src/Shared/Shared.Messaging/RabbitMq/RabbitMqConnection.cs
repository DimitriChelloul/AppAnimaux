namespace Shared.Messaging.RabbitMq;

using Microsoft.Extensions.Options;
using RabbitMQ.Client;

public interface IRabbitMqConnection
{
    Task<IConnection> GetConnectionAsync(CancellationToken ct = default);
}

public sealed class RabbitMqConnection : IRabbitMqConnection, IDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        var o = options.Value;
        _factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            VirtualHost = o.VirtualHost
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        _connection ??= await _factory.CreateConnectionAsync(ct);
        return _connection;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}

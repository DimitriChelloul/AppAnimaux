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
    private readonly SemaphoreSlim _sync = new(1, 1);
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
            VirtualHost = o.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is null || !_connection.IsOpen)
        {
            await _sync.WaitAsync(ct);
            try
            {
                if (_connection is null || !_connection.IsOpen)
                {
                    _connection?.Dispose();
                    _connection = await _factory.CreateConnectionAsync(ct);

                }
            }
            finally
            {
                _sync.Release();
            }
        }

        return _connection;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
        _sync.Dispose();
    }
}

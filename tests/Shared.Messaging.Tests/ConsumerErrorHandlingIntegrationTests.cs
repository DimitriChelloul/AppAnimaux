using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using RabbitMQ.Client;
using Shared.Messaging.Consuming;
using Shared.Messaging.RabbitMq;
using Shared.Persistence.Postgres;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Shared.Messaging.Tests;

public sealed class ConsumerErrorHandlingIntegrationTests
{
    [Fact]
    public async Task Unknown_and_malformed_events_are_dead_lettered_without_blocking_the_queue()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("consumer_test").WithUsername("app").WithPassword("app-test-password").Build();
        await using var rabbit = new RabbitMqBuilder("rabbitmq:4.1-alpine")
            .WithUsername("app").WithPassword("app-test-password").Build();
        await Task.WhenAll(postgres.StartAsync(), rabbit.StartAsync());

        await using (var database = new NpgsqlConnection(postgres.GetConnectionString()))
        {
            await database.OpenAsync();
            await using var command = new NpgsqlCommand(
                "CREATE TABLE inbox_messages(message_id uuid PRIMARY KEY, event_type varchar(200) NOT NULL, processed_on timestamptz NOT NULL)", database);
            await command.ExecuteNonQueryAsync();
        }

        var queue = $"unknown.events.{Guid.NewGuid():N}";
        var options = Options.Create(new RabbitMqOptions
        {
            HostName = rabbit.Hostname,
            Port = rabbit.GetMappedPublicPort(5672),
            UserName = "app",
            Password = "app-test-password",
            ExchangeName = "appanimaux.events",
            QueueName = queue,
            Bindings = ["unknown.#"]
        });
        var connection = new RabbitMqConnection(options);
        var factory = new NpgsqlConnectionFactory(Options.Create(new PostgresOptions { ConnectionString = postgres.GetConnectionString() }));
        var consumer = new RabbitMqConsumerHostedService(
            connection, options, new EventHandlerRegistry(), factory,
            NullLogger<RabbitMqConsumerHostedService>.Instance);

        await consumer.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        var publisher = new RabbitMqEventPublisher(connection);
        var unknown = JsonSerializer.Serialize(new { type = "Unknown.Event", version = 1, data = new { }, occurredOn = DateTimeOffset.UtcNow, messageId = Guid.NewGuid() });
        await publisher.PublishAsync("appanimaux.events", "unknown.event.v1", Encoding.UTF8.GetBytes(unknown));
        await publisher.PublishAsync("appanimaux.events", "unknown.malformed.v1", Encoding.UTF8.GetBytes("{not-json"));

        var rawConnection = await connection.GetConnectionAsync();
        await using var channel = await rawConnection.CreateChannelAsync();
        var deadLetters = 0;
        for (var attempt = 0; attempt < 50 && deadLetters < 2; attempt++)
        {
            var message = await channel.BasicGetAsync($"{queue}.dead-letter", autoAck: true);
            if (message is null) await Task.Delay(100);
            else deadLetters++;
        }

        Assert.Equal(2, deadLetters);
        await consumer.StopAsync(CancellationToken.None);
        connection.Dispose();
    }
}

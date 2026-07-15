using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Contracts.Messaging;
using Shared.Messaging.RabbitMq;
using Testcontainers.RabbitMq;

namespace Shared.Messaging.Tests;

public sealed class RabbitMqPublishingIntegrationTests
{
    [Fact]
    public async Task Confirmed_user_registered_publication_reaches_the_bound_queue()
    {
        await using var rabbit = new RabbitMqBuilder("rabbitmq:4.1-alpine")
            .WithUsername("app")
            .WithPassword("app-test-password")
            .Build();
        await rabbit.StartAsync();

        var options = Options.Create(new RabbitMqOptions
        {
            HostName = rabbit.Hostname,
            Port = rabbit.GetMappedPublicPort(5672),
            UserName = "app",
            Password = "app-test-password",
            ExchangeName = "appanimaux.events"
        });

        using var connection = new RabbitMqConnection(options);
        var rawConnection = await connection.GetConnectionAsync();
        await using var channel = await rawConnection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync("appanimaux.events", ExchangeType.Topic, durable: true, autoDelete: false);
        await channel.QueueDeclareAsync("userprofile.events.test", durable: false, exclusive: false, autoDelete: true);
        await channel.QueueBindAsync("userprofile.events.test", "appanimaux.events", RoutingKeys.Users.Registered);

        var body = Encoding.UTF8.GetBytes("{\"type\":\"User.Registered\"}");
        await new RabbitMqEventPublisher(connection).PublishAsync(
            "appanimaux.events", RoutingKeys.Users.Registered, body);

        BasicGetResult? delivered = null;
        for (var attempt = 0; attempt < 20 && delivered is null; attempt++)
        {
            delivered = await channel.BasicGetAsync("userprofile.events.test", autoAck: false);
            if (delivered is null) await Task.Delay(100);
        }

        Assert.NotNull(delivered);
        Assert.Equal(body, delivered.Body.ToArray());
        await channel.BasicAckAsync(delivered.DeliveryTag, multiple: false);
    }
}

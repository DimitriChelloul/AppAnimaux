using System.Text.Json;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.Users;
using Shared.Contracts.Messaging;
using Shared.Messaging.Consuming;
using Shared.Messaging.Routing;
using Shared.Messaging.Serialization;

namespace Shared.Messaging.Tests;

public sealed class MessagingUnitTests
{
    [Theory]
    [InlineData(EventTypes.Users.UserRegistered, RoutingKeys.Users.Registered)]
    [InlineData(EventTypes.Payments.PaymentSucceeded, RoutingKeys.Payments.Succeeded)]
    [InlineData(EventTypes.HelpRequests.HelpOfferCreated, RoutingKeys.HelpRequests.OfferCreated)]
    [InlineData(EventTypes.HelpRequests.HelpOfferAccepted, RoutingKeys.HelpRequests.OfferAccepted)]
    [InlineData(EventTypes.Messaging.MessageSent, RoutingKeys.Messaging.MessageSent)]
    public void Routing_mapper_maps_supported_events(string eventType, string expected)
        => Assert.Equal(expected, new DefaultEventRoutingMapper().GetRoutingKey(eventType));

    [Fact]
    public void Routing_mapper_rejects_unpublished_local_events()
        => Assert.Throws<InvalidOperationException>(() => new DefaultEventRoutingMapper().GetRoutingKey("PetService.Created"));

    [Fact]
    public void Event_envelope_round_trips_with_the_shared_json_options()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var envelope = new EventEnvelope<UserRegisteredEvent>(EventTypes.Users.UserRegistered, EventTypes.V1,
            new UserRegisteredEvent { UserId = userId, Email = "user@appanimaux.test" }, DateTimeOffset.UtcNow, messageId);

        var json = JsonSerializer.Serialize(envelope, JsonDefaults.Options);
        var restored = JsonSerializer.Deserialize<EventEnvelope<UserRegisteredEvent>>(json, JsonDefaults.Options);

        Assert.NotNull(restored);
        Assert.Equal(messageId, restored.MessageId);
        Assert.Equal(userId, restored.Data.UserId);
        Assert.Equal(EventTypes.Users.UserRegistered, restored.Type);
    }

    [Fact]
    public async Task Handler_registry_selects_only_the_registered_handler()
    {
        var registry = new EventHandlerRegistry();
        var handled = false;
        registry.Register(EventTypes.Users.UserRegistered, (_, _) => { handled = true; return Task.CompletedTask; });

        Assert.True(registry.TryGet(EventTypes.Users.UserRegistered, out var handler));
        Assert.False(registry.TryGet(EventTypes.Payments.PaymentSucceeded, out _));
        await handler("{}", CancellationToken.None);
        Assert.True(handled);
    }

    [Fact]
    public void Publisher_headers_propagate_message_and_distributed_correlation_metadata()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var row = new Shared.Messaging.Outbox.OutboxRow
        {
            MessageId = messageId,
            Type = EventTypes.Users.UserRegistered,
            Payload = JsonSerializer.Serialize(new { data = new { correlationId, causationId, sourceService = "IdentityService" } })
        };

        var headers = Shared.Messaging.Outbox.OutboxPublisherHostedService.BuildHeaders(row);

        Assert.Equal(messageId.ToString(), headers["message_id"]);
        Assert.Equal(EventTypes.Users.UserRegistered, headers["event_type"]);
        Assert.Equal(correlationId.ToString(), headers["correlation_id"]);
        Assert.Equal(causationId.ToString(), headers["causation_id"]);
        Assert.Equal("IdentityService", headers["producer"]);
    }}

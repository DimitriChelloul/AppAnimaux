using Microsoft.OpenApi;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.Payments;
using Shared.Contracts.Messaging;
using Shared.Messaging.Abstractions;
using Shared.Messaging.Consuming;
using Shared.Messaging.Outbox;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Routing;
using Shared.Messaging.Serialization;
using Shared.Persistence.Extensions;
using SubscriptionService.BLL.Handlers;
using SubscriptionService.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "SubscriptionService", Version = "v1" }));

builder.Services.AddPostgresPersistence(builder.Configuration);

// DAL
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<SubscriptionService.DAL.Queries.SubscriptionQueries>();


// Handler typed
builder.Services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, PaymentSucceededHandler>();

// RabbitMQ
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<Shared.Messaging.RabbitMq.IRabbitMqConnection, Shared.Messaging.RabbitMq.RabbitMqConnection>();
builder.Services.AddSingleton<IEventPublisher, Shared.Messaging.RabbitMq.RabbitMqEventPublisher>();
builder.Services.AddSingleton<IEventRoutingMapper, DefaultEventRoutingMapper>();

// Registry : map EventTypes.Payments.PaymentSucceeded -> handler
builder.Services.AddSingleton<IEventHandlerRegistry>(sp =>
{
    var map = new Dictionary<string, Func<string, CancellationToken, Task>>(StringComparer.Ordinal);

    map[EventTypes.Payments.PaymentSucceeded] = async (json, ct) =>
    {
        var env = System.Text.Json.JsonSerializer.Deserialize<EventEnvelope<PaymentSucceededEvent>>(json, JsonDefaults.Options)
                  ?? throw new InvalidOperationException("Envelope null");

        var handler = sp.GetRequiredService<IIntegrationEventHandler<PaymentSucceededEvent>>();
        await handler.HandleAsync(env.Data, ct);
    };

    var registry = new EventHandlerRegistry();
    foreach (var kvp in map)
    {
        registry.Register(kvp.Key, kvp.Value);
    }
    return registry;
});

// Consumer + Outbox Publisher
builder.Services.AddHostedService<Shared.Messaging.RabbitMq.RabbitMqConsumerHostedService>();
builder.Services.AddHostedService<OutboxPublisherHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Seed plans au démarrage (simple)
using (var scope = app.Services.CreateScope())
{
    var plans = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
    await plans.SeedDefaultsIfEmptyAsync(CancellationToken.None);
}

app.MapControllers();
app.Run();

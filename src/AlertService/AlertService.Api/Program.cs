using AlertService.BLL.Handlers;
using AlertService.BLL.Services;
using AlertService.DAL.Repositories;
using Microsoft.OpenApi;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.HelpRequests;
using Shared.Contracts.Events.Messaging;
using Shared.Contracts.Messaging;
using Shared.Messaging.Consuming;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Serialization;
using Shared.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "AlertService", Version = "v1" }));
builder.Services.AddPostgresPersistence(builder.Configuration);

builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationAppService, NotificationAppService>();
builder.Services.AddSingleton<HelpRequestNotificationHandler>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
builder.Services.AddSingleton<IEventHandlerRegistry>(sp =>
{
    var registry = new EventHandlerRegistry();

    registry.Register(EventTypes.HelpRequests.HelpOfferCreated, async (json, ct) =>
    {
        var envelope = System.Text.Json.JsonSerializer.Deserialize<EventEnvelope<HelpOfferCreatedEvent>>(json, JsonDefaults.Options)
                       ?? throw new InvalidOperationException("Envelope null");
        var handler = sp.GetRequiredService<HelpRequestNotificationHandler>();
        await handler.HandleHelpOfferCreatedAsync(envelope.Data, ct);
    });

    registry.Register(EventTypes.HelpRequests.HelpOfferAccepted, async (json, ct) =>
    {
        var envelope = System.Text.Json.JsonSerializer.Deserialize<EventEnvelope<HelpOfferAcceptedEvent>>(json, JsonDefaults.Options)
                       ?? throw new InvalidOperationException("Envelope null");
        var handler = sp.GetRequiredService<HelpRequestNotificationHandler>();
        await handler.HandleHelpOfferAcceptedAsync(envelope.Data, ct);
    });

    registry.Register(EventTypes.Messaging.MessageSent, async (json, ct) =>
    {
        var envelope = System.Text.Json.JsonSerializer.Deserialize<EventEnvelope<MessageSentEvent>>(json, JsonDefaults.Options)
                       ?? throw new InvalidOperationException("Envelope null");
        var handler = sp.GetRequiredService<HelpRequestNotificationHandler>();
        await handler.HandleMessageSentAsync(envelope.Data, ct);
    });

    return registry;
});
builder.Services.AddHostedService<RabbitMqConsumerHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

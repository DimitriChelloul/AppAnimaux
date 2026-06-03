using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.Users;
using Shared.Contracts.Messaging;
using Shared.Messaging.Abstractions;
using Shared.Messaging.Consuming;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Serialization;
using Shared.Persistence.Extensions;
using UserProfileService.BLL.Handlers;
using UserProfileService.BLL.Services;
using UserProfileService.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IUserProfileAppService, UserProfileAppService>();
builder.Services.AddScoped<IIntegrationEventHandler<UserRegisteredEvent>, UserRegisteredHandler>();
builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
builder.Services.AddSingleton<IEventHandlerRegistry>(sp =>
{
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    var registry = new EventHandlerRegistry();

    registry.Register(EventTypes.Users.UserRegistered, async (json, ct) =>
    {
        var envelope = System.Text.Json.JsonSerializer.Deserialize<EventEnvelope<UserRegisteredEvent>>(json, JsonDefaults.Options)
                       ?? throw new InvalidOperationException("Envelope is null.");

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<UserRegisteredEvent>>();
        await handler.HandleAsync(envelope.Data, ct);
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

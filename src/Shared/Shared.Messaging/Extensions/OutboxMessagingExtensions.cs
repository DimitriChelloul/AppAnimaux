using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Abstractions;
using Shared.Messaging.Outbox;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Routing;

namespace Shared.Messaging.Extensions;

public static class OutboxMessagingExtensions
{
    public static IServiceCollection AddOutboxMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddSingleton<IEventRoutingMapper, DefaultEventRoutingMapper>();
        services.AddHostedService<OutboxPublisherHostedService>();
        return services;
    }

    public static IApplicationBuilder UseGenericMutationOutbox(
        this IApplicationBuilder app,
        string serviceName)
        => app.UseMiddleware<GenericMutationOutboxMiddleware>(serviceName);
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultObservability(this IServiceCollection services, Action<ObservabilityOptions>? configure = null)
    {
        var options = new ObservabilityOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddHealthChecks();

        if (options.EnableHttpLogging)
        {
            services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders;
            });
        }

        return services;
    }

    public static IApplicationBuilder UseDefaultObservability(this IApplicationBuilder app)
    {
        return app.UseHealthChecks("/health");
    }
}
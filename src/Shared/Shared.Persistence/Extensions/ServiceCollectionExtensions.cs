using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence.Abstractions;
using Shared.Persistence.Postgres;

namespace Shared.Persistence.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Convention: ConnectionStrings:Default dans chaque microservice
        var cs = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Default'.");

        services.Configure<PostgresOptions>(o => { o = new PostgresOptions { ConnectionString = cs }; });

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        // UnitOfWork en scoped (1 par request/operation)
        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

        return services;
    }
}


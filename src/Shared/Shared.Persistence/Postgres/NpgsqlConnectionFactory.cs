using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Persistence.Postgres;

using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using Shared.Persistence.Abstractions;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _cs;

    public NpgsqlConnectionFactory(IOptions<PostgresOptions> options)
    {
        _cs = options.Value.ConnectionString ?? throw new ArgumentNullException(nameof(options.Value.ConnectionString));
    }

    public IDbConnection Create() => new NpgsqlConnection(_cs);
}


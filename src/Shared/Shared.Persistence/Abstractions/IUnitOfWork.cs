using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Persistence.Abstractions;

using System.Data;

public interface IUnitOfWork : IAsyncDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}


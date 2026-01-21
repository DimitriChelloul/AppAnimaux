namespace Shared.Persistence.Postgres;

using System.Data;
using Shared.Persistence.Abstractions;

public sealed class DapperUnitOfWork : IUnitOfWork
{
    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }

    private bool _completed;

    public DapperUnitOfWork(IDbConnectionFactory factory)
    {
        Connection = factory.Create();
        Connection.Open();
        Transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        if (_completed) return Task.CompletedTask;
        Transaction.Commit();
        _completed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        if (_completed) return Task.CompletedTask;
        Transaction.Rollback();
        _completed = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (!_completed)
            {
                // sécurité : rollback si pas de commit explicite
                Transaction.Rollback();
            }
        }
        catch { /* ignore */ }

        Transaction.Dispose();
        Connection.Dispose();
        return ValueTask.CompletedTask;
    }
}


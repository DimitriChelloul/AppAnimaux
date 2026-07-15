using System.Transactions;

namespace Shared.Persistence.Transactions;

public static class TransactionalOutbox
{
    public static async Task ExecuteAsync(Func<Task> action)
    {
        using var scope = CreateScope();
        await action();
        scope.Complete();
    }

    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        using var scope = CreateScope();
        var result = await action();
        scope.Complete();
        return result;
    }

    private static TransactionScope CreateScope() => new(
        TransactionScopeOption.Required,
        new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.DefaultTimeout
        },
        TransactionScopeAsyncFlowOption.Enabled);
}

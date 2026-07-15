using Microsoft.AspNetCore.Builder;

namespace Shared.Persistence.Transactions;

public static class TransactionalOutboxMiddlewareExtensions
{
    public static IApplicationBuilder UseTransactionalOutbox(this IApplicationBuilder app)
        => app.Use(async (_, next) => await TransactionalOutbox.ExecuteAsync(next));
}

namespace PaymentService.DAL.UnitOfWork;

public interface IPaymentUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}

public sealed class PaymentUnitOfWork : IPaymentUnitOfWork
{
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct)
        => operation(ct);
}

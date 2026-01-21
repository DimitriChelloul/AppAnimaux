namespace SubscriptionService.DAL.Repositories;

using SubscriptionService.Domain.Entities;

public interface ISubscriptionRepository
{
    Task<Guid> InsertAsync(Subscription sub, CancellationToken ct);
}


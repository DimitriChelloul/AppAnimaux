namespace SubscriptionService.DAL.Repositories;

using SubscriptionService.Domain.Entities;

public interface IPlanRepository
{
    Task<Plan?> GetByCodeAsync(string code, CancellationToken ct);
    Task SeedDefaultsIfEmptyAsync(CancellationToken ct);
}

namespace SubscriptionService.DAL.Repositories;

using Dapper;
using Shared.Persistence.Abstractions;
using SubscriptionService.Domain.Entities;

public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly IDbConnectionFactory _db;
    public SubscriptionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> InsertAsync(Subscription sub, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var id = sub.Id == Guid.Empty ? Guid.NewGuid() : sub.Id;

        await cn.ExecuteAsync(
            """
            INSERT INTO subscriptions(
              id, user_id, plan_id, status,
              start_at, current_period_start, current_period_end,
              cancel_at_period_end, created_at, updated_at
            )
            VALUES(
              @Id, @UserId, @PlanId, @Status,
              @StartAt, @CurrentPeriodStart, @CurrentPeriodEnd,
              false, now(), now()
            )
            """,
            new
            {
                Id = id,
                sub.UserId,
                sub.PlanId,
                sub.Status,
                StartAt = sub.StartAt,
                CurrentPeriodStart = sub.CurrentPeriodStart,
                CurrentPeriodEnd = sub.CurrentPeriodEnd
            });

        return id;
    }
}


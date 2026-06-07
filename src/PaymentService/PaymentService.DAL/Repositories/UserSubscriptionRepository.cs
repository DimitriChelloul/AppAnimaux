namespace PaymentService.DAL.Repositories;

using Dapper;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Shared.Persistence.Abstractions;

public sealed class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly IDbConnectionFactory _db;
    public UserSubscriptionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<UserSubscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        return await cn.QuerySingleOrDefaultAsync<UserSubscription>(
            """
            SELECT id, user_id AS userId, plan_id AS planId, provider, external_subscription_id AS externalSubscriptionId,
                   external_customer_id AS externalCustomerId, status, current_period_start AS currentPeriodStart,
                   current_period_end AS currentPeriodEnd, auto_renew AS autoRenew, canceled_at AS canceledAt,
                   created_at AS createdAt, updated_at AS updatedAt
            FROM user_subscriptions
            WHERE user_id = @UserId
            ORDER BY updated_at DESC
            LIMIT 1
            """,
            new { UserId = userId });
    }

    public async Task<UserSubscription?> GetByExternalIdAsync(SubscriptionProvider provider, string externalSubscriptionId, CancellationToken ct)
    {
        using var cn = _db.Create();
        return await cn.QuerySingleOrDefaultAsync<UserSubscription>(
            """
            SELECT id, user_id AS userId, plan_id AS planId, provider, external_subscription_id AS externalSubscriptionId,
                   external_customer_id AS externalCustomerId, status, current_period_start AS currentPeriodStart,
                   current_period_end AS currentPeriodEnd, auto_renew AS autoRenew, canceled_at AS canceledAt,
                   created_at AS createdAt, updated_at AS updatedAt
            FROM user_subscriptions
            WHERE provider = @Provider AND external_subscription_id = @ExternalSubscriptionId
            """,
            new { Provider = provider.ToString(), ExternalSubscriptionId = externalSubscriptionId });
    }

    public async Task UpsertAsync(UserSubscription s, CancellationToken ct)
    {
        using var cn = _db.Create();
        await cn.ExecuteAsync(
            """
            INSERT INTO user_subscriptions(id, user_id, plan_id, provider, external_subscription_id, external_customer_id,
                                           status, current_period_start, current_period_end, auto_renew, canceled_at, created_at, updated_at)
            VALUES (@Id, @UserId, @PlanId, @Provider, @ExternalSubscriptionId, @ExternalCustomerId,
                    @Status, @CurrentPeriodStart, @CurrentPeriodEnd, @AutoRenew, @CanceledAt, now(), now())
            ON CONFLICT (id) DO UPDATE SET
                plan_id = EXCLUDED.plan_id,
                provider = EXCLUDED.provider,
                external_subscription_id = EXCLUDED.external_subscription_id,
                external_customer_id = EXCLUDED.external_customer_id,
                status = EXCLUDED.status,
                current_period_start = EXCLUDED.current_period_start,
                current_period_end = EXCLUDED.current_period_end,
                auto_renew = EXCLUDED.auto_renew,
                canceled_at = EXCLUDED.canceled_at,
                updated_at = now()
            """,
            new
            {
                s.Id,
                s.UserId,
                s.PlanId,
                Provider = s.Provider.ToString(),
                s.ExternalSubscriptionId,
                s.ExternalCustomerId,
                Status = s.Status.ToString(),
                s.CurrentPeriodStart,
                s.CurrentPeriodEnd,
                s.AutoRenew,
                s.CanceledAt
            });
    }

    public async Task<IReadOnlyList<UserSubscription>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        var rows = await cn.QueryAsync<UserSubscription>(
            """
            SELECT id, user_id AS userId, plan_id AS planId, provider, external_subscription_id AS externalSubscriptionId,
                   external_customer_id AS externalCustomerId, status, current_period_start AS currentPeriodStart,
                   current_period_end AS currentPeriodEnd, auto_renew AS autoRenew, canceled_at AS canceledAt,
                   created_at AS createdAt, updated_at AS updatedAt
            FROM user_subscriptions
            ORDER BY updated_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { PageSize = pageSize, Offset = Math.Max(0, page - 1) * pageSize });
        return rows.ToList();
    }
}

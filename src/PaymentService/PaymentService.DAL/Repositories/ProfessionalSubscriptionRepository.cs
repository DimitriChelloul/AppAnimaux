namespace PaymentService.DAL.Repositories;

using Dapper;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using Shared.Persistence.Abstractions;

public sealed class ProfessionalSubscriptionRepository : IProfessionalSubscriptionRepository
{
    private readonly IDbConnectionFactory _db;
    public ProfessionalSubscriptionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ProfessionalSubscription?> GetByProfessionalIdAsync(Guid professionalId, CancellationToken ct)
    {
        using var cn = _db.Create();
        return await cn.QuerySingleOrDefaultAsync<ProfessionalSubscription>(
            """
            SELECT id, professional_id AS professionalId, plan_id AS planId, stripe_customer_id AS stripeCustomerId,
                   stripe_subscription_id AS stripeSubscriptionId, status, current_period_start AS currentPeriodStart,
                   current_period_end AS currentPeriodEnd, auto_renew AS autoRenew, canceled_at AS canceledAt,
                   created_at AS createdAt, updated_at AS updatedAt
            FROM professional_subscriptions
            WHERE professional_id = @ProfessionalId
            ORDER BY updated_at DESC
            LIMIT 1
            """,
            new { ProfessionalId = professionalId });
    }

    public async Task<ProfessionalSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct)
    {
        using var cn = _db.Create();
        return await cn.QuerySingleOrDefaultAsync<ProfessionalSubscription>(
            """
            SELECT id, professional_id AS professionalId, plan_id AS planId, stripe_customer_id AS stripeCustomerId,
                   stripe_subscription_id AS stripeSubscriptionId, status, current_period_start AS currentPeriodStart,
                   current_period_end AS currentPeriodEnd, auto_renew AS autoRenew, canceled_at AS canceledAt,
                   created_at AS createdAt, updated_at AS updatedAt
            FROM professional_subscriptions
            WHERE stripe_subscription_id = @StripeSubscriptionId
            """,
            new { StripeSubscriptionId = stripeSubscriptionId });
    }

    public async Task UpsertAsync(ProfessionalSubscription s, CancellationToken ct)
    {
        using var cn = _db.Create();
        await cn.ExecuteAsync(
            """
            INSERT INTO professional_subscriptions(id, professional_id, plan_id, stripe_customer_id, stripe_subscription_id,
                                                   status, current_period_start, current_period_end, auto_renew, canceled_at, created_at, updated_at)
            VALUES (@Id, @ProfessionalId, @PlanId, @StripeCustomerId, @StripeSubscriptionId,
                    @Status, @CurrentPeriodStart, @CurrentPeriodEnd, @AutoRenew, @CanceledAt, now(), now())
            ON CONFLICT (id) DO UPDATE SET
                plan_id = EXCLUDED.plan_id,
                stripe_customer_id = EXCLUDED.stripe_customer_id,
                stripe_subscription_id = EXCLUDED.stripe_subscription_id,
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
                s.ProfessionalId,
                s.PlanId,
                s.StripeCustomerId,
                s.StripeSubscriptionId,
                Status = s.Status.ToString(),
                s.CurrentPeriodStart,
                s.CurrentPeriodEnd,
                s.AutoRenew,
                s.CanceledAt
            });
    }

    public async Task<IReadOnlyList<ProfessionalSubscription>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        var rows = await cn.QueryAsync<ProfessionalSubscription>(
            """
            SELECT id, professional_id AS professionalId, plan_id AS planId, stripe_customer_id AS stripeCustomerId,
                   stripe_subscription_id AS stripeSubscriptionId, status, current_period_start AS currentPeriodStart,
                   current_period_end AS currentPeriodEnd, auto_renew AS autoRenew, canceled_at AS canceledAt,
                   created_at AS createdAt, updated_at AS updatedAt
            FROM professional_subscriptions
            ORDER BY updated_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { PageSize = pageSize, Offset = Math.Max(0, page - 1) * pageSize });
        return rows.ToList();
    }
}

namespace PaymentService.DAL.Repositories;

using Dapper;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Shared.Persistence.Abstractions;

public sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly IDbConnectionFactory _db;
    public SubscriptionPlanRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<SubscriptionPlan>> GetByOwnerTypeAsync(SubscriptionOwnerType ownerType, CancellationToken ct)
    {
        using var cn = _db.Create();
        var rows = await cn.QueryAsync<SubscriptionPlan>(
            """
            SELECT id, code, name, owner_type AS ownerType, provider, price_amount AS priceAmount,
                   currency, billing_period AS billingPeriod, stripe_price_id AS stripePriceId,
                   apple_product_id AS appleProductId, google_product_id AS googleProductId,
                   is_active AS isActive, created_at AS createdAt, updated_at AS updatedAt
            FROM subscription_plans
            WHERE owner_type = @OwnerType AND is_active = true
            ORDER BY price_amount, code
            """,
            new { OwnerType = ownerType.ToString() });
        return rows.ToList();
    }

    public async Task<SubscriptionPlan?> GetByCodeAsync(PlanCode code, CancellationToken ct)
    {
        using var cn = _db.Create();
        return await cn.QuerySingleOrDefaultAsync<SubscriptionPlan>(
            """
            SELECT id, code, name, owner_type AS ownerType, provider, price_amount AS priceAmount,
                   currency, billing_period AS billingPeriod, stripe_price_id AS stripePriceId,
                   apple_product_id AS appleProductId, google_product_id AS googleProductId,
                   is_active AS isActive, created_at AS createdAt, updated_at AS updatedAt
            FROM subscription_plans
            WHERE code = @Code AND is_active = true
            """,
            new { Code = code.ToString() });
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var cn = _db.Create();
        return await cn.QuerySingleOrDefaultAsync<SubscriptionPlan>(
            """
            SELECT id, code, name, owner_type AS ownerType, provider, price_amount AS priceAmount,
                   currency, billing_period AS billingPeriod, stripe_price_id AS stripePriceId,
                   apple_product_id AS appleProductId, google_product_id AS googleProductId,
                   is_active AS isActive, created_at AS createdAt, updated_at AS updatedAt
            FROM subscription_plans
            WHERE id = @Id
            """,
            new { Id = id });
    }

    public async Task<SubscriptionPlan?> GetByProviderProductAsync(SubscriptionProvider provider, string productId, CancellationToken ct)
    {
        using var cn = _db.Create();
        var column = provider == SubscriptionProvider.Apple ? "apple_product_id" :
            provider == SubscriptionProvider.Google ? "google_product_id" : "stripe_price_id";
        return await cn.QuerySingleOrDefaultAsync<SubscriptionPlan>(
            $"""
            SELECT id, code, name, owner_type AS ownerType, provider, price_amount AS priceAmount,
                   currency, billing_period AS billingPeriod, stripe_price_id AS stripePriceId,
                   apple_product_id AS appleProductId, google_product_id AS googleProductId,
                   is_active AS isActive, created_at AS createdAt, updated_at AS updatedAt
            FROM subscription_plans
            WHERE {column} = @ProductId AND is_active = true
            """,
            new { ProductId = productId });
    }
}

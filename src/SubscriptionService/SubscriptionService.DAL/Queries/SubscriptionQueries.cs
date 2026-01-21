using System;
using System.Collections.Generic;
using System.Text;

namespace SubscriptionService.DAL.Queries;

using Dapper;
using Shared.Persistence.Abstractions;

public sealed class SubscriptionQueries
{
    private readonly IDbConnectionFactory _db;
    public SubscriptionQueries(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<dynamic>> GetPlansAsync()
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QueryAsync(
            "SELECT id, code, name, description, price_amount, currency, period, is_active, features FROM plans ORDER BY price_amount;");
    }

    public async Task<dynamic?> GetActiveSubscriptionAsync(Guid userId)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync(
            """
            SELECT s.id, s.user_id, s.status, s.current_period_start, s.current_period_end,
                   p.code AS plan_code, p.name AS plan_name, p.price_amount, p.currency, p.period
            FROM subscriptions s
            JOIN plans p ON p.id = s.plan_id
            WHERE s.user_id = @UserId
              AND s.status IN ('trialing','active','past_due')
            ORDER BY s.created_at DESC
            LIMIT 1
            """,
            new { UserId = userId });
    }
}


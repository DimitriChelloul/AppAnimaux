namespace SubscriptionService.DAL.Repositories;

using Dapper;
using Shared.Persistence.Abstractions;
using SubscriptionService.Domain.Entities;

public sealed class PlanRepository : IPlanRepository
{
    private readonly IDbConnectionFactory _db;
    public PlanRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Plan?> GetByCodeAsync(string code, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<Plan>(
            """
            SELECT id AS Id, code AS Code, name AS Name,
                   price_amount AS PriceAmount, currency AS Currency,
                   period AS Period, is_active AS IsActive
            FROM plans
            WHERE code = @Code AND is_active = true
            """,
            new { Code = code });
    }

    public async Task SeedDefaultsIfEmptyAsync(CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var count = await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM plans;");
        if (count > 0) return;

        // Seed minimal (à adapter)
        await cn.ExecuteAsync(
            """
            INSERT INTO plans(code,name,description,price_amount,currency,period,is_active,features)
            VALUES
              ('FREE','Free','Offre gratuite',0,'EUR','monthly',true,'{"adsFree":false,"monthlyCredits":0}'::jsonb),
              ('BASIC','Basic','Offre Basic',4.99,'EUR','monthly',true,'{"adsFree":true,"monthlyCredits":50}'::jsonb),
              ('PREMIUM','Premium','Offre Premium',9.99,'EUR','monthly',true,'{"adsFree":true,"monthlyCredits":200}'::jsonb)
            ;
            """);
    }
}


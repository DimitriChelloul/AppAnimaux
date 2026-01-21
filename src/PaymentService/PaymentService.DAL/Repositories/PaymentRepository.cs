using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.DAL.Repositories;

using Dapper;
using PaymentService.Domain.Entities;
using Shared.Persistence.Abstractions;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _db;
    public PaymentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> InsertAsync(Payment p, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var id = p.Id == Guid.Empty ? Guid.NewGuid() : p.Id;

        await cn.ExecuteAsync(
            """
            INSERT INTO payments (
                id, user_id, provider, provider_payment_id,
                amount, currency, status,
                purpose_type, purpose_id,
                created_at
            )
            VALUES (
                @Id, @UserId, @Provider, @ProviderPaymentId,
                @Amount, @Currency, @Status,
                @PurposeType, @PurposeId,
                now()
            )
            """,
            new
            {
                Id = id,
                p.UserId,
                p.Provider,
                p.ProviderPaymentId,
                p.Amount,
                p.Currency,
                p.Status,
                p.PurposeType,
                p.PurposeId
            });

        return id;
    }
}


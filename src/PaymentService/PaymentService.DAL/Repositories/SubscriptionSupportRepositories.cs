namespace PaymentService.DAL.Repositories;

using Dapper;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using Shared.Persistence.Abstractions;

public sealed class SubscriptionInvoiceRepository : ISubscriptionInvoiceRepository
{
    private readonly IDbConnectionFactory _db;
    public SubscriptionInvoiceRepository(IDbConnectionFactory db) => _db = db;

    public async Task UpsertAsync(SubscriptionInvoice i, CancellationToken ct)
    {
        using var cn = _db.Create();
        await cn.ExecuteAsync(
            """
            INSERT INTO subscription_invoices(id, subscription_owner_type, subscription_id, provider, external_invoice_id,
                                              amount, currency, status, invoice_url, paid_at, created_at)
            VALUES (@Id, @SubscriptionOwnerType, @SubscriptionId, @Provider, @ExternalInvoiceId,
                    @Amount, @Currency, @Status, @InvoiceUrl, @PaidAt, now())
            ON CONFLICT (provider, external_invoice_id) WHERE external_invoice_id IS NOT NULL DO UPDATE SET
                status = EXCLUDED.status,
                invoice_url = EXCLUDED.invoice_url,
                paid_at = EXCLUDED.paid_at
            """,
            new
            {
                i.Id,
                SubscriptionOwnerType = i.SubscriptionOwnerType.ToString(),
                i.SubscriptionId,
                Provider = i.Provider.ToString(),
                i.ExternalInvoiceId,
                i.Amount,
                i.Currency,
                Status = i.Status.ToString(),
                i.InvoiceUrl,
                i.PaidAt
            });
    }
}

public sealed class SubscriptionEntitlementRepository : ISubscriptionEntitlementRepository
{
    private readonly IDbConnectionFactory _db;
    public SubscriptionEntitlementRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<SubscriptionEntitlement>> GetByPlanIdAsync(Guid planId, CancellationToken ct)
    {
        using var cn = _db.Create();
        var rows = await cn.QueryAsync<SubscriptionEntitlement>(
            """
            SELECT id, plan_id AS planId, key, value, created_at AS createdAt
            FROM subscription_entitlements
            WHERE plan_id = @PlanId
            ORDER BY key
            """,
            new { PlanId = planId });
        return rows.ToList();
    }
}

public sealed class ExternalPurchaseReceiptRepository : IExternalPurchaseReceiptRepository
{
    private readonly IDbConnectionFactory _db;
    public ExternalPurchaseReceiptRepository(IDbConnectionFactory db) => _db = db;

    public async Task InsertAsync(ExternalPurchaseReceipt r, CancellationToken ct)
    {
        using var cn = _db.Create();
        await cn.ExecuteAsync(
            """
            INSERT INTO external_purchase_receipts(id, user_id, provider, product_id, transaction_id,
                                                   original_transaction_id, purchase_token, raw_receipt,
                                                   validation_status, expires_at, created_at)
            VALUES (@Id, @UserId, @Provider, @ProductId, @TransactionId,
                    @OriginalTransactionId, @PurchaseToken, CAST(@RawReceipt AS jsonb),
                    @ValidationStatus, @ExpiresAt, now())
            """,
            new
            {
                r.Id,
                r.UserId,
                Provider = r.Provider.ToString(),
                r.ProductId,
                r.TransactionId,
                r.OriginalTransactionId,
                r.PurchaseToken,
                r.RawReceipt,
                r.ValidationStatus,
                r.ExpiresAt
            });
    }
}

public sealed class WebhookEventRepository : IWebhookEventRepository
{
    private readonly IDbConnectionFactory _db;
    public WebhookEventRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> InsertAsync(WebhookEvent e, CancellationToken ct)
    {
        using var cn = _db.Create();
        var id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id;
        await cn.ExecuteAsync(
            """
            INSERT INTO webhook_events(id, provider, event_type, external_event_id, payload, processed, processed_at, created_at)
            VALUES (@Id, @Provider, @EventType, @ExternalEventId, CAST(@Payload AS jsonb), false, null, now())
            ON CONFLICT (provider, external_event_id) WHERE external_event_id IS NOT NULL DO NOTHING
            """,
            new { Id = id, Provider = e.Provider.ToString(), e.EventType, e.ExternalEventId, e.Payload });
        return id;
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct)
    {
        using var cn = _db.Create();
        await cn.ExecuteAsync(
            "UPDATE webhook_events SET processed = true, processed_at = now() WHERE id = @Id",
            new { Id = id });
    }
}

public sealed class PaymentAuditLogRepository : IPaymentAuditLogRepository
{
    private readonly IDbConnectionFactory _db;
    public PaymentAuditLogRepository(IDbConnectionFactory db) => _db = db;

    public async Task InsertAsync(PaymentAuditLog l, CancellationToken ct)
    {
        using var cn = _db.Create();
        await cn.ExecuteAsync(
            """
            INSERT INTO payment_audit_logs(id, owner_type, owner_id, action, provider, details, created_at)
            VALUES (@Id, @OwnerType, @OwnerId, @Action, @Provider, CAST(@Details AS jsonb), now())
            """,
            new
            {
                Id = l.Id == Guid.Empty ? Guid.NewGuid() : l.Id,
                OwnerType = l.OwnerType.ToString(),
                l.OwnerId,
                l.Action,
                Provider = l.Provider?.ToString(),
                l.Details
            });
    }
}

namespace PaymentService.DAL.Interfaces;

using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public interface ISubscriptionPlanRepository
{
    Task<IReadOnlyList<SubscriptionPlan>> GetByOwnerTypeAsync(SubscriptionOwnerType ownerType, CancellationToken ct);
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<SubscriptionPlan?> GetByCodeAsync(PlanCode code, CancellationToken ct);
    Task<SubscriptionPlan?> GetByProviderProductAsync(SubscriptionProvider provider, string productId, CancellationToken ct);
}

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);
    Task<UserSubscription?> GetByExternalIdAsync(SubscriptionProvider provider, string externalSubscriptionId, CancellationToken ct);
    Task UpsertAsync(UserSubscription subscription, CancellationToken ct);
    Task<IReadOnlyList<UserSubscription>> ListAsync(int page, int pageSize, CancellationToken ct);
}

public interface IProfessionalSubscriptionRepository
{
    Task<ProfessionalSubscription?> GetByProfessionalIdAsync(Guid professionalId, CancellationToken ct);
    Task<ProfessionalSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct);
    Task UpsertAsync(ProfessionalSubscription subscription, CancellationToken ct);
    Task<IReadOnlyList<ProfessionalSubscription>> ListAsync(int page, int pageSize, CancellationToken ct);
}

public interface ISubscriptionInvoiceRepository
{
    Task UpsertAsync(SubscriptionInvoice invoice, CancellationToken ct);
}

public interface ISubscriptionEntitlementRepository
{
    Task<IReadOnlyList<SubscriptionEntitlement>> GetByPlanIdAsync(Guid planId, CancellationToken ct);
}

public interface IExternalPurchaseReceiptRepository
{
    Task InsertAsync(ExternalPurchaseReceipt receipt, CancellationToken ct);
}

public interface IWebhookEventRepository
{
    Task<Guid> InsertAsync(WebhookEvent webhookEvent, CancellationToken ct);
    Task MarkProcessedAsync(Guid id, CancellationToken ct);
}

public interface IPaymentAuditLogRepository
{
    Task InsertAsync(PaymentAuditLog auditLog, CancellationToken ct);
}

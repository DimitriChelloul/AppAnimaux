namespace PaymentService.BLL.Interfaces;

using PaymentService.BLL.DTOs;
using PaymentService.Domain.Enums;

public interface IUserSubscriptionService
{
    Task<SubscriptionStatusDto> GetMineAsync(Guid userId, CancellationToken ct);
    Task<SubscriptionStatusDto> CreateOrUpdateAsync(CreateUserSubscriptionDto dto, string externalSubscriptionId, string? externalCustomerId, DateTimeOffset expiresAt, CancellationToken ct);
    Task<SubscriptionStatusDto> CancelAsync(Guid userId, CancellationToken ct);
}

public interface IProfessionalSubscriptionService
{
    Task<SubscriptionStatusDto> GetMineAsync(Guid professionalId, CancellationToken ct);
    Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(CreateProfessionalSubscriptionDto dto, CancellationToken ct);
    Task<StripePortalSessionDto> CreatePortalSessionAsync(Guid professionalId, CancellationToken ct);
    Task<SubscriptionStatusDto> ChangePlanAsync(ChangeProfessionalPlanDto dto, CancellationToken ct);
    Task<SubscriptionStatusDto> CancelAsync(Guid professionalId, CancellationToken ct);
    Task<SubscriptionStatusDto> UpsertFromStripeAsync(Guid professionalId, PlanCode planCode, string stripeCustomerId, string stripeSubscriptionId, SubscriptionStatus status, DateTimeOffset? periodStart, DateTimeOffset? periodEnd, CancellationToken ct);
}

public interface ISubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetUserPlansAsync(CancellationToken ct);
    Task<IReadOnlyList<SubscriptionPlanDto>> GetProfessionalPlansAsync(CancellationToken ct);
}

public interface ISubscriptionEntitlementService
{
    Task<SubscriptionEntitlementsDto> GetForUserAsync(Guid userId, CancellationToken ct);
    Task<SubscriptionEntitlementsDto> GetForProfessionalAsync(Guid professionalId, CancellationToken ct);
    Task<IReadOnlyDictionary<string, string>> GetByPlanAsync(Guid planId, CancellationToken ct);
}

public interface IApplePurchaseService
{
    Task<SubscriptionStatusDto> ValidateAsync(ValidateApplePurchaseDto dto, CancellationToken ct);
    Task<WebhookProcessResultDto> ProcessServerNotificationAsync(string payload, CancellationToken ct);
}

public interface IGooglePurchaseService
{
    Task<SubscriptionStatusDto> ValidateAsync(ValidateGooglePurchaseDto dto, CancellationToken ct);
    Task<WebhookProcessResultDto> ProcessServerNotificationAsync(string payload, CancellationToken ct);
}

public interface IStripeBillingService
{
    Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(Guid professionalId, PlanCode planCode, string successUrl, string cancelUrl, CancellationToken ct);
    Task<StripePortalSessionDto> CreatePortalSessionAsync(string stripeCustomerId, CancellationToken ct);
    Task ChangePlanAsync(string stripeSubscriptionId, string stripePriceId, CancellationToken ct);
    Task CancelSubscriptionAsync(string stripeSubscriptionId, bool cancelAtPeriodEnd, CancellationToken ct);
    bool IsValidWebhookSignature(string payload, string? signatureHeader);
}

public interface ISubscriptionWebhookService
{
    Task<WebhookProcessResultDto> ProcessStripeAsync(string payload, string? signatureHeader, CancellationToken ct);
    Task<WebhookProcessResultDto> ProcessAppleAsync(string payload, CancellationToken ct);
    Task<WebhookProcessResultDto> ProcessGoogleAsync(string payload, CancellationToken ct);
}

public interface IPaymentAuditService
{
    Task LogAsync(SubscriptionOwnerType ownerType, Guid ownerId, string action, SubscriptionProvider? provider, object details, CancellationToken ct);
}

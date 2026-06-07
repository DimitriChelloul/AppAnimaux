namespace PaymentService.BLL.DTOs;

using PaymentService.Domain.Enums;

public sealed record CreateUserSubscriptionDto(
    Guid UserId,
    PlanCode PlanCode,
    SubscriptionProvider Provider);

public sealed record ValidateApplePurchaseDto(
    Guid UserId,
    string ProductId,
    string? TransactionId,
    string? OriginalTransactionId,
    string? ReceiptData);

public sealed record ValidateGooglePurchaseDto(
    Guid UserId,
    string ProductId,
    string PurchaseToken,
    string PackageName);

public sealed record CreateProfessionalSubscriptionDto(
    Guid ProfessionalId,
    PlanCode PlanCode,
    string SuccessUrl,
    string CancelUrl);

public sealed record ChangeProfessionalPlanDto(
    Guid ProfessionalId,
    PlanCode PlanCode);

public sealed record CancelSubscriptionDto(
    Guid OwnerId,
    SubscriptionOwnerType OwnerType,
    bool CancelAtPeriodEnd = true);

public sealed record SubscriptionStatusDto(
    Guid? SubscriptionId,
    Guid OwnerId,
    SubscriptionOwnerType OwnerType,
    PlanCode PlanCode,
    SubscriptionStatus Status,
    DateTimeOffset? CurrentPeriodStart,
    DateTimeOffset? CurrentPeriodEnd,
    bool AutoRenew);

public sealed record SubscriptionPlanDto(
    Guid Id,
    PlanCode Code,
    string Name,
    SubscriptionOwnerType OwnerType,
    SubscriptionProvider? Provider,
    decimal PriceAmount,
    string Currency,
    string BillingPeriod,
    bool IsActive);

public sealed record SubscriptionEntitlementsDto(
    Guid OwnerId,
    SubscriptionOwnerType OwnerType,
    PlanCode PlanCode,
    IReadOnlyDictionary<string, string> Entitlements);

public sealed record StripeCheckoutSessionDto(
    string SessionId,
    string Url);

public sealed record StripePortalSessionDto(
    string Url);

public sealed record WebhookProcessResultDto(
    bool Processed,
    string Message);

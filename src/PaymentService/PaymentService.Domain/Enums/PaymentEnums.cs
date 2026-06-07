namespace PaymentService.Domain.Enums;

public enum SubscriptionOwnerType
{
    User = 1,
    Professional = 2
}

public enum SubscriptionProvider
{
    Apple = 1,
    Google = 2,
    Stripe = 3
}

public enum SubscriptionStatus
{
    Pending = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Expired = 5,
    Failed = 6
}

public enum PlanCode
{
    Free = 1,
    UserPremium = 2,
    UserPlus = 3,
    ProFree = 4,
    ProBasic = 5,
    ProPlus = 6,
    ProPremium = 7
}

public enum InvoiceStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public enum PaymentEventType
{
    UserSubscriptionCreated = 1,
    UserSubscriptionRenewed = 2,
    UserSubscriptionCanceled = 3,
    UserSubscriptionExpired = 4,
    UserSubscriptionPaymentFailed = 5,
    ProfessionalSubscriptionCreated = 6,
    ProfessionalSubscriptionRenewed = 7,
    ProfessionalSubscriptionCanceled = 8,
    ProfessionalSubscriptionExpired = 9,
    ProfessionalSubscriptionPaymentFailed = 10,
    ProfessionalPlanChanged = 11,
    SubscriptionEntitlementsChanged = 12,
    WebhookReceived = 13,
    WebhookProcessed = 14,
    PurchaseValidated = 15
}

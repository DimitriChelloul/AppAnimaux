namespace PaymentService.BLL.Options;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ProfessionalSuccessUrl { get; set; } = string.Empty;
    public string ProfessionalCancelUrl { get; set; } = string.Empty;
    public string CustomerPortalReturnUrl { get; set; } = string.Empty;
}

public sealed class AppleOptions
{
    public string BundleId { get; set; } = string.Empty;
    public string IssuerId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox";
    public bool AllowSimulatedValidation { get; set; } = true;
}

public sealed class GooglePlayOptions
{
    public string PackageName { get; set; } = string.Empty;
    public string ServiceAccountJsonPath { get; set; } = string.Empty;
    public bool AllowSimulatedValidation { get; set; } = true;
}

public sealed class PaymentOptions
{
    public string DefaultCurrency { get; set; } = "EUR";
    public int TrialDays { get; set; }
    public int GracePeriodDays { get; set; } = 3;
}

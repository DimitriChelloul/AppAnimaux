namespace PaymentService.Domain.Entities;

using PaymentService.Domain.Enums;

public sealed class PaymentProviderCustomer
{
    public Guid Id { get; set; }
    public SubscriptionOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    public SubscriptionProvider Provider { get; set; }
    public string ExternalCustomerId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

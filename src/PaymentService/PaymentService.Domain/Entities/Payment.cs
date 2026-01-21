using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.Domain.Entities;

public sealed class Payment
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Status { get; init; } = "succeeded";
    public string Provider { get; init; } = "stripe";
    public string? ProviderPaymentId { get; init; }

    public string PurposeType { get; init; } = default!; // subscription/credits/...
    public Guid? PurposeId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

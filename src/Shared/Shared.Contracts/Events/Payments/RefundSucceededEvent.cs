using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Payments;

using Shared.Contracts.Events.Abstractions;

public record RefundSucceededEvent : IntegrationEvent
{
    public Guid RefundId { get; init; }
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }

    public string Provider { get; init; } = "stripe";
    public string? ProviderRefundId { get; init; } // re_...
    public string? ProviderPaymentId { get; init; } // pi_... / paypal txn

    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";

    public string PurposeType { get; init; } = default!;
    public Guid? PurposeId { get; init; }

    public string? Reason { get; init; }
}


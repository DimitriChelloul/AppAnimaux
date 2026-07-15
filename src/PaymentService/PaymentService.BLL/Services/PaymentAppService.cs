using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.BLL.Services;

using System.Text.Json;
using PaymentService.DAL.Repositories;
using PaymentService.Domain.Entities;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.Payments;
using Shared.Contracts.Messaging;
using Shared.Messaging.Outbox;

public sealed class PaymentAppService : IPaymentAppService
{
    private readonly IPaymentRepository _payments;
    private readonly IOutboxRepository _outbox;

    public PaymentAppService(IPaymentRepository payments, IOutboxRepository outbox)
    {
        _payments = payments;
        _outbox = outbox;
    }

    public async Task<Guid> SimulateSuccessAsync(Guid userId, string planCode, decimal amount, CancellationToken ct)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Currency = "EUR",
            Status = "succeeded",
            Provider = "stripe",
            ProviderPaymentId = $"pi_sim_{Guid.NewGuid():N}",
            PurposeType = "subscription",
            PurposeId = null
        };

        var paymentId = await _payments.InsertAsync(payment, ct);

        var messageId = Guid.NewGuid();
        var evt = new PaymentSucceededEvent
        {
            PaymentId = payment.Id,
            UserId = userId,
            Amount = amount,
            Currency = "EUR",
            PurposeType = "subscription",
            PurposeId = null,
            Provider = "stripe",
            ProviderPaymentId = payment.ProviderPaymentId,
            PlanCode = planCode
        };

        var env = new EventEnvelope<PaymentSucceededEvent>(
            Type: EventTypes.Payments.PaymentSucceeded,
            Version: EventTypes.V1,
            OccurredOn: DateTimeOffset.UtcNow,
            Data: evt,
            MessageId: messageId
        );

        var payloadJson = JsonSerializer.Serialize(env, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _outbox.AddAsync(messageId, EventTypes.Payments.PaymentSucceeded, payloadJson, "payment", paymentId, ct);

        return paymentId;
    }
}


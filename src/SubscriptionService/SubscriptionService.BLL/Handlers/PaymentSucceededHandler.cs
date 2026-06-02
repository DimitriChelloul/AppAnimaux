namespace SubscriptionService.BLL.Handlers;

using System.Text.Json;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.Payments;
using Shared.Contracts.Events.Subscriptions;
using Shared.Contracts.Messaging;
using Shared.Messaging.Abstractions;
using SubscriptionService.DAL.Repositories;
using SubscriptionService.Domain.Entities;

public sealed class PaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IPlanRepository _plans;
    private readonly ISubscriptionRepository _subs;
    private readonly IOutboxRepository _outbox;

    public PaymentSucceededHandler(IPlanRepository plans, ISubscriptionRepository subs, IOutboxRepository outbox)
    {
        _plans = plans;
        _subs = subs;
        _outbox = outbox;
    }

    public async Task HandleAsync(PaymentSucceededEvent evt, CancellationToken ct)
    {
        // MVP : on ne traite que les paiements "subscription"
        if (!string.Equals(evt.PurposeType, "subscription", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(evt.PlanCode))
            throw new InvalidOperationException("PlanCode is required for subscription activation.");

        var plan = await _plans.GetByCodeAsync(evt.PlanCode, ct)
                   ?? throw new InvalidOperationException($"Unknown plan code '{evt.PlanCode}'.");

        var now = DateTimeOffset.UtcNow;
        var end = plan.Period switch
        {
            "yearly" => now.AddYears(1),
            "one_time" => now, // à toi de décider ; ici pas de période
            _ => now.AddMonths(1)
        };

        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = evt.UserId,
            PlanId = plan.Id,
            Status = "active",
            StartAt = now,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = end
        };

        var subId = await _subs.InsertAsync(sub, ct);

        // Outbox event Subscription.Activated (optionnel mais très utile)
        var messageId = Guid.NewGuid();
        var activated = new SubscriptionActivatedEvent
        {
            SubscriptionId = subId,
            UserId = evt.UserId,
            PlanId = plan.Id,
            PlanCode = plan.Code,
            Features = new PlanFeatures(),
            CurrentPeriodStart = now,
            CurrentPeriodEnd = end
        };

        var env = new EventEnvelope<SubscriptionActivatedEvent>(
            Type: EventTypes.Subscriptions.SubscriptionActivated,
            Version: EventTypes.V1,
            OccurredOn: DateTimeOffset.UtcNow,
            Data: activated,
            MessageId: messageId
        );

        var payloadJson = JsonSerializer.Serialize(env, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _outbox.AddAsync(messageId, EventTypes.Subscriptions.SubscriptionActivated, payloadJson, "subscription", subId, ct);
    }
}


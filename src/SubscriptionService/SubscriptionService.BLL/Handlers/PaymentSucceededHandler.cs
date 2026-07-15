namespace SubscriptionService.BLL.Handlers;

using Shared.Contracts.Events.Payments;
using Shared.Messaging.Abstractions;
using SubscriptionService.DAL.Repositories;
using SubscriptionService.Domain.Entities;

public sealed class PaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IPlanRepository _plans;
    private readonly ISubscriptionRepository _subs;

    public PaymentSucceededHandler(IPlanRepository plans, ISubscriptionRepository subs)

    {
        _plans = plans;
        _subs = subs;
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

        await _subs.InsertAsync(sub, ct);
    }
}


namespace PaymentService.BLL.Services;

using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public sealed class UserSubscriptionService : IUserSubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly ISubscriptionPlanRepository _plans;

    public UserSubscriptionService(IUserSubscriptionRepository subscriptions, ISubscriptionPlanRepository plans)

    {
        _subscriptions = subscriptions;
        _plans = plans;
    }

    public async Task<SubscriptionStatusDto> GetMineAsync(Guid userId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetActiveByUserIdAsync(userId, ct);
        if (sub is null)
        {
            return new SubscriptionStatusDto(null, userId, SubscriptionOwnerType.User, PlanCode.Free, SubscriptionStatus.Active, null, null, false);
        }
        var plan = await _plans.GetByIdAsync(sub.PlanId, ct) ?? throw new InvalidOperationException("Plan not found.");
        return Map(userId, plan.Code, sub);
    }

    public async Task<SubscriptionStatusDto> CreateOrUpdateAsync(CreateUserSubscriptionDto dto, string externalSubscriptionId, string? externalCustomerId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        var plan = await _plans.GetByCodeAsync(dto.PlanCode, ct) ?? throw new ArgumentException("Unknown plan.");
        if (plan.OwnerType != SubscriptionOwnerType.User) throw new ArgumentException("Plan is not a user plan.");

        var existing = await _subscriptions.GetByExternalIdAsync(dto.Provider, externalSubscriptionId, ct)
            ?? await _subscriptions.GetActiveByUserIdAsync(dto.UserId, ct);
        var sub = existing ?? new UserSubscription { Id = Guid.NewGuid(), UserId = dto.UserId };
        sub.PlanId = plan.Id;
        sub.Provider = dto.Provider;
        sub.ExternalSubscriptionId = externalSubscriptionId;
        sub.ExternalCustomerId = externalCustomerId;
        sub.Status = expiresAt > DateTimeOffset.UtcNow ? SubscriptionStatus.Active : SubscriptionStatus.Expired;
        sub.CurrentPeriodStart = DateTimeOffset.UtcNow;
        sub.CurrentPeriodEnd = expiresAt;
        sub.AutoRenew = true;
        await _subscriptions.UpsertAsync(sub, ct);

        return Map(dto.UserId, plan.Code, sub);
    }

    public async Task<SubscriptionStatusDto> CancelAsync(Guid userId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetActiveByUserIdAsync(userId, ct) ?? throw new InvalidOperationException("No subscription.");
        sub.Status = SubscriptionStatus.Canceled;
        sub.AutoRenew = false;
        sub.CanceledAt = DateTimeOffset.UtcNow;
        await _subscriptions.UpsertAsync(sub, ct);
        var plan = await _plans.GetByIdAsync(sub.PlanId, ct) ?? throw new InvalidOperationException("Plan not found.");
        return Map(userId, plan.Code, sub);
    }

    private static SubscriptionStatusDto Map(Guid userId, PlanCode planCode, UserSubscription s)
        => new(s.Id, userId, SubscriptionOwnerType.User, planCode, s.Status, s.CurrentPeriodStart, s.CurrentPeriodEnd, s.AutoRenew);
}

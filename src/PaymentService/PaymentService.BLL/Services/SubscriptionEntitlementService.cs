namespace PaymentService.BLL.Services;

using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Enums;

public sealed class SubscriptionEntitlementService : ISubscriptionEntitlementService
{
    private readonly IUserSubscriptionRepository _users;
    private readonly IProfessionalSubscriptionRepository _professionals;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly ISubscriptionEntitlementRepository _entitlements;

    public SubscriptionEntitlementService(
        IUserSubscriptionRepository users,
        IProfessionalSubscriptionRepository professionals,
        ISubscriptionPlanRepository plans,
        ISubscriptionEntitlementRepository entitlements)
    {
        _users = users;
        _professionals = professionals;
        _plans = plans;
        _entitlements = entitlements;
    }

    public async Task<SubscriptionEntitlementsDto> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var sub = await _users.GetActiveByUserIdAsync(userId, ct);
        var plan = sub is null
            ? await _plans.GetByCodeAsync(PlanCode.Free, ct)
            : await _plans.GetByIdAsync(sub.PlanId, ct);
        if (plan is null) throw new InvalidOperationException("Default user plan is missing.");
        return new SubscriptionEntitlementsDto(userId, SubscriptionOwnerType.User, plan.Code, await GetByPlanAsync(plan.Id, ct));
    }

    public async Task<SubscriptionEntitlementsDto> GetForProfessionalAsync(Guid professionalId, CancellationToken ct)
    {
        var sub = await _professionals.GetByProfessionalIdAsync(professionalId, ct);
        var plan = sub is null
            ? await _plans.GetByCodeAsync(PlanCode.ProFree, ct)
            : await _plans.GetByIdAsync(sub.PlanId, ct);
        if (plan is null) throw new InvalidOperationException("Default professional plan is missing.");
        return new SubscriptionEntitlementsDto(professionalId, SubscriptionOwnerType.Professional, plan.Code, await GetByPlanAsync(plan.Id, ct));
    }

    public async Task<IReadOnlyDictionary<string, string>> GetByPlanAsync(Guid planId, CancellationToken ct)
        => (await _entitlements.GetByPlanIdAsync(planId, ct)).ToDictionary(x => x.Key, x => x.Value);
}

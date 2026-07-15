namespace PaymentService.BLL.Services;

using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public sealed class ProfessionalSubscriptionService : IProfessionalSubscriptionService
{
    private readonly IProfessionalSubscriptionRepository _subscriptions;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly IStripeBillingService _stripe;

    public ProfessionalSubscriptionService(IProfessionalSubscriptionRepository subscriptions, ISubscriptionPlanRepository plans, IStripeBillingService stripe)

    {
        _subscriptions = subscriptions;
        _plans = plans;
        _stripe = stripe;
    }

    public async Task<SubscriptionStatusDto> GetMineAsync(Guid professionalId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetByProfessionalIdAsync(professionalId, ct);
        if (sub is null) return new SubscriptionStatusDto(null, professionalId, SubscriptionOwnerType.Professional, PlanCode.ProFree, SubscriptionStatus.Active, null, null, false);
        var plan = await _plans.GetByIdAsync(sub.PlanId, ct) ?? throw new InvalidOperationException("Plan not found.");
        return Map(professionalId, plan.Code, sub);
    }

    public Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(CreateProfessionalSubscriptionDto dto, CancellationToken ct)
        => _stripe.CreateCheckoutSessionAsync(dto.ProfessionalId, dto.PlanCode, dto.SuccessUrl, dto.CancelUrl, ct);

    public async Task<StripePortalSessionDto> CreatePortalSessionAsync(Guid professionalId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetByProfessionalIdAsync(professionalId, ct) ?? throw new InvalidOperationException("No professional subscription.");
        if (string.IsNullOrWhiteSpace(sub.StripeCustomerId)) throw new InvalidOperationException("Missing Stripe customer.");
        return await _stripe.CreatePortalSessionAsync(sub.StripeCustomerId, ct);
    }

    public async Task<SubscriptionStatusDto> ChangePlanAsync(ChangeProfessionalPlanDto dto, CancellationToken ct)
    {
        var plan = await _plans.GetByCodeAsync(dto.PlanCode, ct) ?? throw new ArgumentException("Unknown plan.");
        var sub = await _subscriptions.GetByProfessionalIdAsync(dto.ProfessionalId, ct) ?? throw new InvalidOperationException("No professional subscription.");
        if (!string.IsNullOrWhiteSpace(sub.StripeSubscriptionId) && !string.IsNullOrWhiteSpace(plan.StripePriceId))
        {
            await _stripe.ChangePlanAsync(sub.StripeSubscriptionId, plan.StripePriceId, ct);
        }
        sub.PlanId = plan.Id;
        await _subscriptions.UpsertAsync(sub, ct);
        return Map(dto.ProfessionalId, plan.Code, sub);
    }

    public async Task<SubscriptionStatusDto> CancelAsync(Guid professionalId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetByProfessionalIdAsync(professionalId, ct) ?? throw new InvalidOperationException("No professional subscription.");
        if (!string.IsNullOrWhiteSpace(sub.StripeSubscriptionId))
        {
            await _stripe.CancelSubscriptionAsync(sub.StripeSubscriptionId, true, ct);
        }
        sub.Status = SubscriptionStatus.Canceled;
        sub.AutoRenew = false;
        sub.CanceledAt = DateTimeOffset.UtcNow;
        await _subscriptions.UpsertAsync(sub, ct);
        var plan = await _plans.GetByIdAsync(sub.PlanId, ct) ?? throw new InvalidOperationException("Plan not found.");
        return Map(professionalId, plan.Code, sub);
    }

    public async Task<SubscriptionStatusDto> UpsertFromStripeAsync(Guid professionalId, PlanCode planCode, string stripeCustomerId, string stripeSubscriptionId, SubscriptionStatus status, DateTimeOffset? periodStart, DateTimeOffset? periodEnd, CancellationToken ct)
    {
        var plan = await _plans.GetByCodeAsync(planCode, ct) ?? throw new ArgumentException("Unknown plan.");
        var sub = await _subscriptions.GetByProfessionalIdAsync(professionalId, ct) ?? new ProfessionalSubscription { Id = Guid.NewGuid(), ProfessionalId = professionalId };
        sub.PlanId = plan.Id;
        sub.StripeCustomerId = stripeCustomerId;
        sub.StripeSubscriptionId = stripeSubscriptionId;
        sub.Status = status;
        sub.CurrentPeriodStart = periodStart;
        sub.CurrentPeriodEnd = periodEnd;
        sub.AutoRenew = status is SubscriptionStatus.Active or SubscriptionStatus.PastDue;
        await _subscriptions.UpsertAsync(sub, ct);
        return Map(professionalId, plan.Code, sub);
    }

    private static SubscriptionStatusDto Map(Guid professionalId, PlanCode planCode, ProfessionalSubscription s)
        => new(s.Id, professionalId, SubscriptionOwnerType.Professional, planCode, s.Status, s.CurrentPeriodStart, s.CurrentPeriodEnd, s.AutoRenew);
}

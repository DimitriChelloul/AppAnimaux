namespace PaymentService.BLL.Services;

using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _plans;
    public SubscriptionPlanService(ISubscriptionPlanRepository plans) => _plans = plans;

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetUserPlansAsync(CancellationToken ct)
        => (await _plans.GetByOwnerTypeAsync(SubscriptionOwnerType.User, ct)).Select(Map).ToList();

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetProfessionalPlansAsync(CancellationToken ct)
        => (await _plans.GetByOwnerTypeAsync(SubscriptionOwnerType.Professional, ct)).Select(Map).ToList();

    private static SubscriptionPlanDto Map(SubscriptionPlan p)
        => new(p.Id, p.Code, p.Name, p.OwnerType, p.Provider, p.PriceAmount, p.Currency, p.BillingPeriod, p.IsActive);
}

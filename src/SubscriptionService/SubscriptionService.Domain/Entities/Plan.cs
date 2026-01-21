namespace SubscriptionService.Domain.Entities;

public sealed class Plan
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Period { get; init; } = "monthly"; // monthly/yearly/one_time
    public bool IsActive { get; init; } = true;
}


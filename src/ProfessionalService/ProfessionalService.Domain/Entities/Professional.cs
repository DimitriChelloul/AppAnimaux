namespace ProfessionalService.Domain.Entities;

public sealed class Professional
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string SubscriptionPlan { get; init; } = "none";
    public string SubscriptionStatus { get; init; } = "inactive";
    public bool IsVerified { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

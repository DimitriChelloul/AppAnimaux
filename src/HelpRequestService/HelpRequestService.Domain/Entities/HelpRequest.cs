namespace HelpRequestService.Domain.Entities;

public sealed class HelpRequest
{
    public Guid Id { get; init; }
    public Guid RequesterUserId { get; init; }
    public Guid? PetId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string HelpType { get; init; } = string.Empty;
    public string Status { get; init; } = "draft";
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateTimeOffset? StartAt { get; init; }
    public DateTimeOffset? EndAt { get; init; }
    public bool IsPaid { get; init; }
    public decimal? BudgetAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
}

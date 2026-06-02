namespace HelpRequestService.Domain.Entities;

public sealed class HelpOffer
{
    public Guid Id { get; init; }
    public Guid HelpRequestId { get; init; }
    public Guid HelperUserId { get; init; }
    public string? Message { get; init; }
    public decimal? ProposedAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Status { get; init; } = "pending";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

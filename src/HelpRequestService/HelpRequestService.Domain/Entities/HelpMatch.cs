namespace HelpRequestService.Domain.Entities;

public sealed class HelpMatch
{
    public Guid Id { get; init; }
    public Guid HelpRequestId { get; init; }
    public Guid AcceptedOfferId { get; init; }
    public Guid RequesterUserId { get; init; }
    public Guid HelperUserId { get; init; }
    public string Status { get; init; } = "active";
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancelReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

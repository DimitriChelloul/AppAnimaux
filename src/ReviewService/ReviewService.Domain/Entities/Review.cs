namespace ReviewService.Domain.Entities;

public sealed class Review
{
    public Guid Id { get; init; }
    public Guid HelpMatchId { get; init; }
    public Guid ReviewerUserId { get; init; }
    public Guid RevieweeUserId { get; init; }
    public Guid? PetId { get; init; }
    public short Rating { get; init; }
    public string? Comment { get; init; }
    public bool IsPublic { get; init; }
    public string ModerationStatus { get; init; } = "published";
    public Guid? ModeratedBy { get; init; }
    public DateTimeOffset? ModeratedAt { get; init; }
    public string? ModerationReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

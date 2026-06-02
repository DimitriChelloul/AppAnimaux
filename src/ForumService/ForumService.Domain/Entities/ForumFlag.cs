namespace ForumService.Domain.Entities;

public sealed class ForumFlag
{
    public Guid Id { get; init; }
    public Guid FlaggedByUserId { get; init; }
    public string TargetType { get; init; } = "";
    public Guid TargetId { get; init; }
    public string Reason { get; init; } = "";
    public string? Details { get; init; }
    public string Status { get; init; } = "open";
    public Guid? ReviewedBy { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public string? DecisionNotes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

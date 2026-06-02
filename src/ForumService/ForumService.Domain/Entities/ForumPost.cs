namespace ForumService.Domain.Entities;

public sealed class ForumPost
{
    public Guid Id { get; init; }
    public Guid TopicId { get; init; }
    public Guid AuthorUserId { get; init; }
    public string Content { get; init; } = "";
    public string? AttachmentsJson { get; init; }
    public string Status { get; init; } = "published";
    public DateTimeOffset? EditedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

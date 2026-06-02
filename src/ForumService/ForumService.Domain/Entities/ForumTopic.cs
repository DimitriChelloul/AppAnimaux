namespace ForumService.Domain.Entities;

public sealed class ForumTopic
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public Guid AuthorUserId { get; init; }
    public string Title { get; init; } = "";
    public string? Slug { get; init; }
    public string? Content { get; init; }
    public string? AttachmentsJson { get; init; }
    public string[]? Tags { get; init; }
    public string Status { get; init; } = "open";
    public bool IsPinned { get; init; }
    public long ViewsCount { get; init; }
    public long RepliesCount { get; init; }
    public DateTimeOffset? LastPostAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

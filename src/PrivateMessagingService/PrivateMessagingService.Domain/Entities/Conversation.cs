namespace PrivateMessagingService.Domain.Entities;

public sealed class Conversation
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "dm";
    public string? Title { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset? LastMessageAt { get; init; }
    public Guid? LastMessageId { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

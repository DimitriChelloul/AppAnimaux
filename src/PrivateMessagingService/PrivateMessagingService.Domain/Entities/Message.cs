namespace PrivateMessagingService.Domain.Entities;

public sealed class Message
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string MessageType { get; init; } = "text";
    public string? Content { get; init; }
    public string? Attachments { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? EditedAt { get; init; }
}

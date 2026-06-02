namespace PrivateMessagingService.Domain.Entities;

public sealed class ConversationMember
{
    public Guid ConversationId { get; init; }
    public Guid UserId { get; init; }
    public string Role { get; init; } = "member";
    public DateTimeOffset JoinedAt { get; init; }
    public bool IsMuted { get; init; }
    public DateTimeOffset? MutedUntil { get; init; }
    public bool IsHidden { get; init; }
    public DateTimeOffset? HiddenAt { get; init; }
    public Guid? LastReadMessageId { get; init; }
    public DateTimeOffset? LastReadAt { get; init; }
}

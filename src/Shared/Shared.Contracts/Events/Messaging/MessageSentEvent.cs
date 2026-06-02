using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.Messaging;

public sealed record MessageSentEvent : IntegrationEvent
{
    public Guid ConversationId { get; init; }
    public Guid MessageId { get; init; }
    public Guid SenderUserId { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = Array.Empty<Guid>();
    public string MessageType { get; init; } = "text";
    public string? ContentPreview { get; init; }
}

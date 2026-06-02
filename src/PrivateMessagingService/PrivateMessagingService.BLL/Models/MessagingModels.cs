using PrivateMessagingService.Domain.Entities;

namespace PrivateMessagingService.BLL.Models;

public sealed record CreateConversationRequest(IReadOnlyCollection<Guid> MemberUserIds, string? Title);

public sealed record SendMessageRequest(string? Content, string? MessageType, IReadOnlyCollection<MessageAttachmentRequest>? Attachments);

public sealed record MessageAttachmentRequest(Guid MediaId, string? Url, string? ContentType, string? FileName);

public sealed record MarkConversationReadRequest(Guid? MessageId);

public sealed record ConversationsResponse(IReadOnlyCollection<Conversation> Items, int Count);

public sealed record MessagesResponse(IReadOnlyCollection<Message> Items, int Count);

public sealed record ConversationDetailsResponse(Conversation Conversation, IReadOnlyCollection<Guid> MemberUserIds);

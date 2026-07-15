using System.Text.Json;
using Shared.Messaging.Outbox;
using PrivateMessagingService.BLL.Models;
using PrivateMessagingService.DAL.Repositories;
using PrivateMessagingService.Domain.Entities;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.Messaging;
using Shared.Contracts.Messaging;

namespace PrivateMessagingService.BLL.Services;

public sealed class PrivateMessagingAppService : IPrivateMessagingAppService
{
    private readonly IConversationRepository _conversations;
    private readonly IOutboxRepository _outbox;

    public PrivateMessagingAppService(IConversationRepository conversations, IOutboxRepository outbox)
    {
        _conversations = conversations;
        _outbox = outbox;
    }

    public async Task<ConversationDetailsResponse> CreateConversationAsync(Guid userId, CreateConversationRequest request, CancellationToken ct)
    {
        var members = request.MemberUserIds.Append(userId).Distinct().ToArray();
        if (members.Length < 2)
        {
            throw new ArgumentException("A conversation requires at least two members.");
        }

        var type = members.Length > 2 ? "group" : "dm";
        var title = type == "group" ? NormalizeOptional(request.Title) : null;
        var id = await _conversations.CreateAsync(userId, type, title, members, ct);

        return await GetConversationAsync(userId, id, ct) ?? throw new InvalidOperationException("Conversation could not be loaded.");
    }

    public async Task<ConversationsResponse> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        var items = await _conversations.GetMineAsync(userId, page, pageSize, ct);
        return new ConversationsResponse(items, items.Count);
    }

    public async Task<ConversationDetailsResponse?> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdForUserAsync(conversationId, userId, ct);
        if (conversation is null)
        {
            return null;
        }

        var members = await _conversations.GetMemberIdsAsync(conversationId, ct);
        return new ConversationDetailsResponse(conversation, members);
    }

    public async Task<MessagesResponse> GetMessagesAsync(Guid userId, Guid conversationId, int page, int pageSize, CancellationToken ct)
    {
        var items = await _conversations.GetMessagesAsync(conversationId, userId, page, pageSize, ct);
        return new MessagesResponse(items, items.Count);
    }

    public async Task<Message?> SendMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken ct)
    {
        var messageType = string.IsNullOrWhiteSpace(request.MessageType) ? "text" : request.MessageType.Trim().ToLowerInvariant();
        var content = NormalizeOptional(request.Content);
        var attachmentsJson = SerializeAttachments(request.Attachments);

        if (content is null && request.Attachments is not { Count: > 0 })
        {
            throw new ArgumentException("A message requires content or attachments.");
        }

        var message = await _conversations.AddMessageAsync(conversationId, userId, messageType, content, attachmentsJson, ct);
        if (message is null)
        {
            return null;
        }

        var members = await _conversations.GetMemberIdsAsync(conversationId, ct);
        var recipients = members.Where(id => id != userId).ToArray();
        await AddOutboxAsync(
            EventTypes.Messaging.MessageSent,
            new MessageSentEvent
            {
                ConversationId = conversationId,
                MessageId = message.Id,
                SenderUserId = userId,
                RecipientUserIds = recipients,
                MessageType = message.MessageType,
                ContentPreview = BuildPreview(content),
                SourceService = "PrivateMessagingService"
            },
            "message",
            message.Id,
            ct);

        return message;
    }

    public Task<bool> MarkReadAsync(Guid userId, Guid conversationId, MarkConversationReadRequest request, CancellationToken ct)
    {
        return _conversations.MarkReadAsync(conversationId, userId, request.MessageId, ct);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BuildPreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return content.Length <= 120 ? content : content[..120];
    }

    private static string SerializeAttachments(IReadOnlyCollection<MessageAttachmentRequest>? attachments)
    {
        return JsonSerializer.Serialize(attachments ?? Array.Empty<MessageAttachmentRequest>(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private async Task AddOutboxAsync<T>(string type, T data, string aggregateType, Guid aggregateId, CancellationToken ct)
        where T : IntegrationEvent
    {
        var messageId = Guid.NewGuid();
        var envelope = new EventEnvelope<T>(type, EventTypes.V1, data, DateTimeOffset.UtcNow, messageId);
        var payloadJson = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _outbox.AddAsync(messageId, type, payloadJson, aggregateType, aggregateId, ct);
    }
}

using PrivateMessagingService.BLL.Models;
using PrivateMessagingService.Domain.Entities;

namespace PrivateMessagingService.BLL.Services;

public interface IPrivateMessagingAppService
{
    Task<ConversationDetailsResponse> CreateConversationAsync(Guid userId, CreateConversationRequest request, CancellationToken ct);
    Task<ConversationsResponse> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<ConversationDetailsResponse?> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken ct);
    Task<MessagesResponse> GetMessagesAsync(Guid userId, Guid conversationId, int page, int pageSize, CancellationToken ct);
    Task<Message?> SendMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken ct);
    Task<bool> MarkReadAsync(Guid userId, Guid conversationId, MarkConversationReadRequest request, CancellationToken ct);
}

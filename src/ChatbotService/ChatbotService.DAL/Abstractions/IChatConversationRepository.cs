using ChatbotService.Domain.Entities;

namespace ChatbotService.DAL.Abstractions;

public interface IChatConversationRepository
{
    Task<ChatConversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ChatConversation> CreateAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task TouchAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ConversationSummary?> GetSummaryAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task UpsertSummaryAsync(ConversationSummary summary, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

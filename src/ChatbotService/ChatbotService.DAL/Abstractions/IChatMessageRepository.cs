using ChatbotService.Domain.Entities;

namespace ChatbotService.DAL.Abstractions;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetRecentAsync(Guid conversationId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetAllAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<int> CountByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task DeleteOlderThanLatestAsync(Guid conversationId, int keepLatest, CancellationToken cancellationToken = default);
}

using ChatbotService.Domain.Entities;

namespace ChatbotService.DAL.Abstractions;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetRecentAsync(Guid conversationId, int limit, CancellationToken cancellationToken = default);
}

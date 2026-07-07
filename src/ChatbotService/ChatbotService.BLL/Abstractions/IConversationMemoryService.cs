using ChatbotService.BLL.Models;

namespace ChatbotService.BLL.Abstractions;

public interface IConversationMemoryService
{
    Task<ConversationMemory> GetMemoryAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task SummarizeIfNeededAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

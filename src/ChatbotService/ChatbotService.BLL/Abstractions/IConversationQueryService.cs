using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Abstractions;

public interface IConversationQueryService
{
    Task<ChatbotConversationDto?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

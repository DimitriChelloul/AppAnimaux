using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Abstractions;

public interface IChatbotOrchestrator
{
    Task<AskChatbotResponse> AskAsync(AskChatbotRequest request, CancellationToken cancellationToken = default);
}

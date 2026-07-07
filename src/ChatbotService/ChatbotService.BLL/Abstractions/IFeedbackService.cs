using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Abstractions;

public interface IFeedbackService
{
    Task<ChatbotFeedbackResponse> AddAsync(ChatbotFeedbackRequest request, CancellationToken cancellationToken = default);
}

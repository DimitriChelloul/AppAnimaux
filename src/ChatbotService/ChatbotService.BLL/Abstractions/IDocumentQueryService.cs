using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Abstractions;

public interface IDocumentQueryService
{
    Task<IReadOnlyList<ChatbotDocumentDto>> ListAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<ChatbotStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

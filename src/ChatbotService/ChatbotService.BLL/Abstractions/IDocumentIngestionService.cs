using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Abstractions;

public interface IDocumentIngestionService
{
    Task<Guid> IngestAsync(IngestKnowledgeDocumentRequest request, CancellationToken cancellationToken = default);
    Task<int> ReindexAsync(CancellationToken cancellationToken = default);
}

using ChatbotService.BLL.Abstractions;
using ChatbotService.DAL.Abstractions;
using ChatbotService.DAL.Models;
using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Services;

public sealed class DocumentQueryService : IDocumentQueryService
{
    private readonly IKnowledgeDocumentRepository _documentRepository;

    public DocumentQueryService(IKnowledgeDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<IReadOnlyList<ChatbotDocumentDto>> ListAsync(CancellationToken cancellationToken = default)
        => (await _documentRepository.ListAsync(cancellationToken)).Select(ToDto).ToArray();

    public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _documentRepository.DeleteAsync(documentId, cancellationToken);

    public async Task<ChatbotStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => ToDto(await _documentRepository.GetStatisticsAsync(cancellationToken));

    private static ChatbotDocumentDto ToDto(DocumentListItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        SourceType = item.SourceType,
        SourceUri = item.SourceUri,
        Locale = item.Locale,
        Status = item.Status,
        ChunkCount = item.ChunkCount,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };

    private static ChatbotStatisticsDto ToDto(ChatbotStorageStatistics stats) => new()
    {
        DocumentCount = stats.DocumentCount,
        IndexedDocumentCount = stats.IndexedDocumentCount,
        ChunkCount = stats.ChunkCount,
        EmbeddingCount = stats.EmbeddingCount,
        ConversationCount = stats.ConversationCount,
        MessageCount = stats.MessageCount
    };
}

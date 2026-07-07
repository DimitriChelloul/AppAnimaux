using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.DAL.Models;
using Shared.Semantic;

namespace ChatbotService.DAL.Abstractions;

public interface IKnowledgeDocumentRepository
{
    Task<KnowledgeDocument> AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
    Task<KnowledgeDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocument>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task ReplaceChunksAsync(Guid documentId, IReadOnlyList<string> chunks, string embeddingModel, IReadOnlyList<EmbeddingVector> embeddings, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid documentId, DocumentStatus status, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<ChatbotStorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

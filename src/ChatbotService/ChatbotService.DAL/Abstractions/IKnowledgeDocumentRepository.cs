using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Shared.Semantic;

namespace ChatbotService.DAL.Abstractions;

public interface IKnowledgeDocumentRepository
{
    Task<KnowledgeDocument> AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocument>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task ReplaceChunksAsync(Guid documentId, IReadOnlyList<string> chunks, string embeddingModel, IReadOnlyList<EmbeddingVector> embeddings, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid documentId, DocumentStatus status, CancellationToken cancellationToken = default);
}

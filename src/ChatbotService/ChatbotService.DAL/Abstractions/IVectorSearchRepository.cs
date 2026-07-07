using ChatbotService.Domain.ValueObjects;
using Shared.Semantic;

namespace ChatbotService.DAL.Abstractions;

public interface IVectorSearchRepository
{
    Task<IReadOnlyList<RagSearchResult>> SearchAsync(EmbeddingVector queryEmbedding, int limit, double minSimilarity, CancellationToken cancellationToken = default);
}

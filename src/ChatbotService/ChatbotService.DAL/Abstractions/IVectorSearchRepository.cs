using Shared.Semantic;

namespace ChatbotService.DAL.Abstractions;

public interface IVectorSearchRepository
{
    Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(EmbeddingVector queryEmbedding, int limit, double minSimilarity, CancellationToken cancellationToken = default);
}

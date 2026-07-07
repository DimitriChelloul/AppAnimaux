using Shared.Semantic;

namespace ChatbotService.DAL.Abstractions;

public interface IVectorSearchRepository
{
    Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(
        EmbeddingVector queryEmbedding,
        string queryText,
        int limit,
        double minSimilarity,
        double vectorWeight,
        double textWeight,
        CancellationToken cancellationToken = default);
}

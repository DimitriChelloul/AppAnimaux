using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class DocumentRankingService
{
    public IReadOnlyList<SemanticSearchResult> Rank(IReadOnlyList<SemanticSearchResult> chunks, int maxChunks)
        => chunks.OrderByDescending(chunk => chunk.Similarity).ThenBy(chunk => chunk.ChunkIndex).Take(Math.Max(1, maxChunks)).ToArray();
}

using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class ChunkScoringService
{
    public IReadOnlyList<SemanticSearchResult> RemoveDuplicates(IReadOnlyList<SemanticSearchResult> chunks)
        => chunks.GroupBy(chunk => chunk.ChunkId).Select(group => group.OrderByDescending(x => x.Similarity).First()).ToArray();
}

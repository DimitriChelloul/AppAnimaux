using Shared.Semantic;

namespace ChatbotService.BLL.Models;

public sealed record RagContext(IReadOnlyList<SemanticSearchResult> Chunks, int EstimatedTokens);

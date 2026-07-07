using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Options;
using Microsoft.Extensions.Options;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class RagRetriever : IRagRetriever
{
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ChunkScoringService _chunkScoringService;
    private readonly DocumentRankingService _documentRankingService;
    private readonly TokenBudgetManager _tokenBudgetManager;
    private readonly RagOptions _options;

    public RagRetriever(
        ISemanticSearchService semanticSearchService,
        ChunkScoringService chunkScoringService,
        DocumentRankingService documentRankingService,
        TokenBudgetManager tokenBudgetManager,
        IOptions<RagOptions> options)
    {
        _semanticSearchService = semanticSearchService;
        _chunkScoringService = chunkScoringService;
        _documentRankingService = documentRankingService;
        _tokenBudgetManager = tokenBudgetManager;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<SemanticSearchResult>> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        var searched = await _semanticSearchService.SearchAsync(question, cancellationToken);
        var deduplicated = _chunkScoringService.RemoveDuplicates(searched);
        var ranked = _documentRankingService.Rank(deduplicated, _options.MaxContextChunks);
        return _tokenBudgetManager.TakeWithinBudget(ranked, chunk => chunk.Content, _options.MaxContextTokens);
    }
}

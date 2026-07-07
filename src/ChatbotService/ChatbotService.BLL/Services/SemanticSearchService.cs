using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Observability;
using ChatbotService.BLL.Options;
using ChatbotService.DAL.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class SemanticSearchService : ISemanticSearchService
{
    private readonly EmbeddingService _embeddingService;
    private readonly IVectorSearchRepository _vectorSearchRepository;
    private readonly RagOptions _options;
    private readonly ILogger<SemanticSearchService> _logger;
    private readonly ChatbotMetrics _metrics;

    public SemanticSearchService(EmbeddingService embeddingService, IVectorSearchRepository vectorSearchRepository, IOptions<RagOptions> options, ILogger<SemanticSearchService> logger, ChatbotMetrics metrics)
    {
        _embeddingService = embeddingService;
        _vectorSearchRepository = vectorSearchRepository;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(string question, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GenerateAsync(question, cancellationToken);
        var results = await _vectorSearchRepository.SearchAsync(embedding, question, _options.TopK, _options.MinSimilarity, _options.VectorWeight, _options.TextWeight, cancellationToken);
        _logger.LogInformation("Chatbot vector search returned {ChunkCount} chunks", results.Count);
        _metrics.VectorSearch(results.Count);
        return results;
    }
}

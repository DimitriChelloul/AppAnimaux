using ChatbotService.BLL.Abstractions;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class RagRetriever : IRagRetriever
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorSearchRepository _vectorSearchRepository;
    private readonly IConfiguration _configuration;

    public RagRetriever(IEmbeddingProvider embeddingProvider, IVectorSearchRepository vectorSearchRepository, IConfiguration configuration)
    {
        _embeddingProvider = embeddingProvider;
        _vectorSearchRepository = vectorSearchRepository;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<RagSearchResult>> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingProvider.GenerateEmbeddingAsync(question, cancellationToken);
        var maxChunks = ConfigurationReader.GetInt(_configuration, "Chatbot:MaxRetrievedChunks", 5);
        var minSimilarity = ConfigurationReader.GetDouble(_configuration, "Chatbot:MinSimilarity", 0.70);

        return await _vectorSearchRepository.SearchAsync(embedding, maxChunks, minSimilarity, cancellationToken);
    }
}

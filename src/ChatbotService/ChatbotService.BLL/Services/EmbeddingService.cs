using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class EmbeddingService
{
    private readonly IEmbeddingProvider _embeddingProvider;

    public EmbeddingService(IEmbeddingProvider embeddingProvider)
    {
        _embeddingProvider = embeddingProvider;
    }

    public Task<EmbeddingVector> GenerateAsync(string input, CancellationToken cancellationToken = default)
        => _embeddingProvider.GenerateEmbeddingAsync(input, cancellationToken);
}

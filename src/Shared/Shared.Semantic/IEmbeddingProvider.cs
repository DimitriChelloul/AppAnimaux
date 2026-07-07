namespace Shared.Semantic;

public interface IEmbeddingProvider
{
    Task<EmbeddingVector> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default);
}

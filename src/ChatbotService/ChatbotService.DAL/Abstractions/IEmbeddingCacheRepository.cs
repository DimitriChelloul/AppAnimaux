using Shared.Semantic;

namespace ChatbotService.DAL.Abstractions;

public interface IEmbeddingCacheRepository
{
    Task<EmbeddingVector?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task SetAsync(string cacheKey, string inputHash, string model, EmbeddingVector embedding, CancellationToken cancellationToken = default);
}

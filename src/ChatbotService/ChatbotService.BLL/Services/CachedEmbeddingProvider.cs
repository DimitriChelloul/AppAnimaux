using System.Security.Cryptography;
using System.Text;
using ChatbotService.BLL.Observability;
using ChatbotService.BLL.Options;
using ChatbotService.DAL.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class CachedEmbeddingProvider : IEmbeddingProvider
{
    private readonly IEmbeddingProvider _inner;
    private readonly IEmbeddingCacheRepository _cacheRepository;
    private readonly EmbeddingOptions _options;
    private readonly ChatbotMetrics _metrics;

    public CachedEmbeddingProvider(IEmbeddingProvider inner, IEmbeddingCacheRepository cacheRepository, IOptions<EmbeddingOptions> options, ChatbotMetrics metrics)
    {
        _inner = inner;
        _cacheRepository = cacheRepository;
        _options = options.Value;
        _metrics = metrics;
    }

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        if (!_options.CacheEnabled)
        {
            var generatedWithoutCache = await _inner.GenerateEmbeddingAsync(input, cancellationToken);
            _metrics.Embedding("provider");
            return generatedWithoutCache;
        }

        var hash = ComputeHash(input);
        var key = $"{_options.Provider}:{_options.Model}:{hash}";
        var cached = await _cacheRepository.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            _metrics.Embedding("cache");
            return cached;
        }

        var generated = await _inner.GenerateEmbeddingAsync(input, cancellationToken);
        await _cacheRepository.SetAsync(key, hash, _options.Model, generated, cancellationToken);
        _metrics.Embedding("provider");
        return generated;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

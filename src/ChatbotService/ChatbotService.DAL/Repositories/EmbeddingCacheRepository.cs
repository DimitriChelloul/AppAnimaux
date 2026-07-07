using ChatbotService.DAL.Abstractions;
using Dapper;
using Shared.Persistence.Abstractions;
using Shared.Semantic;

namespace ChatbotService.DAL.Repositories;

public sealed class EmbeddingCacheRepository : IEmbeddingCacheRepository
{
    private readonly IDbConnectionFactory _db;

    public EmbeddingCacheRepository(IDbConnectionFactory db) => _db = db;

    public async Task<EmbeddingVector?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var literal = await cn.QuerySingleOrDefaultAsync<string?>("SELECT embedding::text FROM chatbot_embedding_cache WHERE cache_key = @CacheKey", new { CacheKey = cacheKey });
        return literal is null ? null : ParseVectorLiteral(literal);
    }

    public async Task SetAsync(string cacheKey, string inputHash, string model, EmbeddingVector embedding, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            INSERT INTO chatbot_embedding_cache (cache_key, input_hash, model, embedding, dimensions, created_at)
            VALUES (@CacheKey, @InputHash, @Model, CAST(@Embedding AS vector), @Dimensions, now())
            ON CONFLICT (cache_key) DO NOTHING
            """,
            new { CacheKey = cacheKey, InputHash = inputHash, Model = model, Embedding = ToVectorLiteral(embedding), embedding.Dimensions });
    }

    private static EmbeddingVector ParseVectorLiteral(string literal)
    {
        var values = literal.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(float.Parse).ToArray();
        return new EmbeddingVector(values);
    }

    private static string ToVectorLiteral(EmbeddingVector embedding)
        => "[" + string.Join(",", embedding.Values.Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))) + "]";
}

using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.ValueObjects;
using Dapper;
using Shared.Persistence.Abstractions;
using Shared.Semantic;

namespace ChatbotService.DAL.Repositories;

public sealed class PgVectorSearchRepository : IVectorSearchRepository
{
    private readonly IDbConnectionFactory _db;

    public PgVectorSearchRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<RagSearchResult>> SearchAsync(EmbeddingVector queryEmbedding, int limit, double minSimilarity, CancellationToken cancellationToken = default)
    {
        if (queryEmbedding.Values.Count == 0)
        {
            return Array.Empty<RagSearchResult>();
        }

        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<RagSearchResult>(
            """
            SELECT
                d.id AS DocumentId,
                c.id AS ChunkId,
                d.title AS Title,
                c.content AS Content,
                d.source_uri AS SourceUri,
                1 - (e.embedding <=> CAST(@Embedding AS vector)) AS Similarity,
                c.chunk_index AS ChunkIndex
            FROM chatbot_chunk_embeddings e
            JOIN chatbot_chunks c ON c.id = e.chunk_id
            JOIN chatbot_documents d ON d.id = c.document_id
            WHERE d.status = 'indexed'
              AND 1 - (e.embedding <=> CAST(@Embedding AS vector)) >= @MinSimilarity
            ORDER BY e.embedding <=> CAST(@Embedding AS vector)
            LIMIT @Limit
            """,
            new
            {
                Embedding = ToVectorLiteral(queryEmbedding),
                MinSimilarity = minSimilarity,
                Limit = limit
            });

        return rows.ToArray();
    }

    private static string ToVectorLiteral(EmbeddingVector embedding)
        => "[" + string.Join(",", embedding.Values.Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))) + "]";
}

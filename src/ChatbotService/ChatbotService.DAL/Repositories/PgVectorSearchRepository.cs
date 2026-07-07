using ChatbotService.DAL.Abstractions;
using Dapper;
using Shared.Persistence.Abstractions;
using Shared.Semantic;

namespace ChatbotService.DAL.Repositories;

public sealed class PgVectorSearchRepository : IVectorSearchRepository
{
    private readonly IDbConnectionFactory _db;

    public PgVectorSearchRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(
        EmbeddingVector queryEmbedding,
        string queryText,
        int limit,
        double minSimilarity,
        double vectorWeight,
        double textWeight,
        CancellationToken cancellationToken = default)
    {
        if (queryEmbedding.Values.Count == 0)
        {
            return Array.Empty<SemanticSearchResult>();
        }

        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<SemanticSearchResult>(
            """
            WITH ranked AS (
                SELECT
                    d.id AS DocumentId,
                    c.id AS ChunkId,
                    d.title AS Title,
                    c.content AS Content,
                    d.source_uri AS SourceUri,
                    1 - (e.embedding <=> CAST(@Embedding AS vector)) AS VectorSimilarity,
                    ts_rank_cd(
                        to_tsvector('simple', d.title || ' ' || c.content),
                        plainto_tsquery('simple', @QueryText)
                    ) AS TextRank,
                    c.chunk_index AS ChunkIndex
                FROM chatbot_chunk_embeddings e
                JOIN chatbot_chunks c ON c.id = e.chunk_id
                JOIN chatbot_documents d ON d.id = c.document_id
                WHERE d.status = 'indexed'
            )
            SELECT
                DocumentId,
                ChunkId,
                Title,
                Content,
                SourceUri,
                LEAST(1, GREATEST(0, (@VectorWeight * VectorSimilarity) + (@TextWeight * LEAST(1, TextRank)))) AS Similarity,
                ChunkIndex
            FROM ranked
            WHERE VectorSimilarity >= @MinSimilarity OR TextRank > 0
            ORDER BY ((@VectorWeight * VectorSimilarity) + (@TextWeight * LEAST(1, TextRank))) DESC
            LIMIT @Limit
            """,
            new
            {
                Embedding = ToVectorLiteral(queryEmbedding),
                QueryText = queryText,
                MinSimilarity = minSimilarity,
                VectorWeight = vectorWeight,
                TextWeight = textWeight,
                Limit = Math.Max(1, limit)
            });

        return rows.GroupBy(row => row.ChunkId).Select(group => group.First()).ToArray();
    }

    private static string ToVectorLiteral(EmbeddingVector embedding)
        => "[" + string.Join(",", embedding.Values.Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))) + "]";
}

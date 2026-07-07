using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Dapper;
using ChatbotService.DAL.Models;
using Shared.Persistence.Abstractions;
using Shared.Semantic;

namespace ChatbotService.DAL.Repositories;

public sealed class KnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
    private readonly IDbConnectionFactory _db;

    public KnowledgeDocumentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<KnowledgeDocument> AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var id = document.Id == Guid.Empty ? Guid.NewGuid() : document.Id;
        var row = await cn.QuerySingleAsync<KnowledgeDocumentRow>(
            """
            INSERT INTO chatbot_documents (id, title, content, source_type, source_uri, locale, status, created_at, updated_at)
            VALUES (@Id, @Title, @Content, @SourceType, @SourceUri, @Locale, @Status, now(), now())
            RETURNING id AS Id, title AS Title, content AS Content, source_type AS SourceTypeText,
                      source_uri AS SourceUri, locale AS Locale, status AS StatusText,
                      created_at AS CreatedAt, updated_at AS UpdatedAt
            """,
            new
            {
                Id = id,
                document.Title,
                document.Content,
                SourceType = document.SourceType.ToString().ToLowerInvariant(),
                document.SourceUri,
                document.Locale,
                Status = document.Status.ToString().ToLowerInvariant()
            });

        return row.ToDocument();
    }

    public async Task<KnowledgeDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var row = await cn.QuerySingleOrDefaultAsync<KnowledgeDocumentRow>(
            """
            SELECT id AS Id, title AS Title, content AS Content, source_type AS SourceTypeText,
                   source_uri AS SourceUri, locale AS Locale, status AS StatusText,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM chatbot_documents
            WHERE id = @DocumentId
            """,
            new { DocumentId = documentId });

        return row?.ToDocument();
    }

    public async Task<IReadOnlyList<DocumentListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<DocumentListItem>(
            """
            SELECT d.id AS Id,
                   d.title AS Title,
                   d.source_type AS SourceType,
                   d.source_uri AS SourceUri,
                   d.locale AS Locale,
                   d.status AS Status,
                   count(c.id)::int AS ChunkCount,
                   d.created_at AS CreatedAt,
                   d.updated_at AS UpdatedAt
            FROM chatbot_documents d
            LEFT JOIN chatbot_chunks c ON c.document_id = d.id
            WHERE d.status <> 'archived'
            GROUP BY d.id
            ORDER BY d.created_at DESC
            """);

        return rows.ToArray();
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<KnowledgeDocumentRow>(
            """
            SELECT id AS Id, title AS Title, content AS Content, source_type AS SourceTypeText,
                   source_uri AS SourceUri, locale AS Locale, status AS StatusText,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM chatbot_documents
            WHERE status <> 'archived'
            ORDER BY created_at DESC
            """);

        return rows.Select(row => row.ToDocument()).ToArray();
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var chunks = await cn.QueryAsync<KnowledgeChunk>(
            """
            SELECT id AS Id, document_id AS DocumentId, chunk_index AS ChunkIndex,
                   content AS Content, token_estimate AS TokenEstimate, created_at AS CreatedAt
            FROM chatbot_chunks
            WHERE document_id = @DocumentId
            ORDER BY chunk_index
            """,
            new { DocumentId = documentId });

        return chunks.ToArray();
    }

    public async Task ReplaceChunksAsync(Guid documentId, IReadOnlyList<string> chunks, string embeddingModel, IReadOnlyList<EmbeddingVector> embeddings, CancellationToken cancellationToken = default)
    {
        if (chunks.Count != embeddings.Count)
        {
            throw new ArgumentException("The number of chunks must match the number of embeddings.");
        }

        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        await cn.ExecuteAsync("DELETE FROM chatbot_chunks WHERE document_id = @DocumentId", new { DocumentId = documentId }, tx);

        for (var index = 0; index < chunks.Count; index++)
        {
            var chunkId = Guid.NewGuid();
            await cn.ExecuteAsync(
                """
                INSERT INTO chatbot_chunks (id, document_id, chunk_index, content, token_estimate, created_at)
                VALUES (@Id, @DocumentId, @ChunkIndex, @Content, @TokenEstimate, now())
                """,
                new { Id = chunkId, DocumentId = documentId, ChunkIndex = index, Content = chunks[index], TokenEstimate = EstimateTokens(chunks[index]) },
                tx);

            await cn.ExecuteAsync(
                """
                INSERT INTO chatbot_chunk_embeddings (chunk_id, embedding, model, dimensions, created_at)
                VALUES (@ChunkId, CAST(@Embedding AS vector), @Model, @Dimensions, now())
                """,
                new { ChunkId = chunkId, Embedding = ToVectorLiteral(embeddings[index]), Model = embeddingModel, embeddings[index].Dimensions },
                tx);
        }

        await cn.ExecuteAsync("UPDATE chatbot_documents SET status = 'indexed', updated_at = now() WHERE id = @DocumentId", new { DocumentId = documentId }, tx);
        tx.Commit();
    }

    public async Task UpdateStatusAsync(Guid documentId, DocumentStatus status, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();
        await cn.ExecuteAsync("UPDATE chatbot_documents SET status = @Status, updated_at = now() WHERE id = @DocumentId", new { DocumentId = documentId, Status = status.ToString().ToLowerInvariant() });
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();
        await cn.ExecuteAsync("UPDATE chatbot_documents SET status = 'archived', updated_at = now() WHERE id = @DocumentId", new { DocumentId = documentId });
    }

    public async Task<ChatbotStorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        return new ChatbotStorageStatistics
        {
            DocumentCount = await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_documents WHERE status <> 'archived'"),
            IndexedDocumentCount = await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_documents WHERE status = 'indexed'"),
            ChunkCount = await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_chunks"),
            EmbeddingCount = await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_chunk_embeddings"),
            ConversationCount = await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_conversations"),
            MessageCount = await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_messages")
        };
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);
    private static string ToVectorLiteral(EmbeddingVector embedding) => "[" + string.Join(",", embedding.Values.Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))) + "]";

    private sealed record KnowledgeDocumentRow(Guid Id, string Title, string Content, string SourceTypeText, string? SourceUri, string? Locale, string StatusText, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
    {
        public KnowledgeDocument ToDocument() => new()
        {
            Id = Id,
            Title = Title,
            Content = Content,
            SourceType = Enum.TryParse<KnowledgeSourceType>(SourceTypeText, true, out var sourceType) ? sourceType : KnowledgeSourceType.Internal,
            SourceUri = SourceUri,
            Locale = Locale,
            Status = Enum.TryParse<DocumentStatus>(StatusText, true, out var status) ? status : DocumentStatus.Draft,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}

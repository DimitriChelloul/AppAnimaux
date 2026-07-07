using ChatbotService.BLL.Abstractions;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Shared.Contracts.Chatbot;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IKnowledgeDocumentRepository _documentRepository;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IConfiguration _configuration;

    public DocumentIngestionService(
        IKnowledgeDocumentRepository documentRepository,
        ITextChunker chunker,
        IEmbeddingProvider embeddingProvider,
        IConfiguration configuration)
    {
        _documentRepository = documentRepository;
        _chunker = chunker;
        _embeddingProvider = embeddingProvider;
        _configuration = configuration;
    }

    public async Task<Guid> IngestAsync(IngestKnowledgeDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Document title is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Document content is required.", nameof(request));
        }

        var document = await _documentRepository.AddAsync(new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            SourceType = ParseSourceType(request.SourceType),
            SourceUri = request.SourceUri,
            Locale = request.Locale,
            Status = DocumentStatus.Draft
        }, cancellationToken);

        await IndexDocumentAsync(document, cancellationToken);
        return document.Id;
    }

    public async Task<int> ReindexAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetAllActiveAsync(cancellationToken);
        var count = 0;

        foreach (var document in documents)
        {
            await IndexDocumentAsync(document, cancellationToken);
            count++;
        }

        return count;
    }

    private async Task IndexDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken)
    {
        var chunkSize = ConfigurationReader.GetInt(_configuration, "Chatbot:ChunkSize", 900);
        var chunkOverlap = ConfigurationReader.GetInt(_configuration, "Chatbot:ChunkOverlap", 150);
        var embeddingModel = ConfigurationReader.GetString(_configuration, "OpenAi:EmbeddingModel", "text-embedding-3-small");
        var chunks = _chunker.Chunk(document.Content, chunkSize, chunkOverlap);

        if (chunks.Count == 0)
        {
            await _documentRepository.UpdateStatusAsync(document.Id, DocumentStatus.Failed, cancellationToken);
            return;
        }

        var embeddings = new List<EmbeddingVector>(chunks.Count);
        foreach (var chunk in chunks)
        {
            embeddings.Add(await _embeddingProvider.GenerateEmbeddingAsync(chunk, cancellationToken));
        }

        await _documentRepository.ReplaceChunksAsync(document.Id, chunks, embeddingModel, embeddings, cancellationToken);
    }

    private static KnowledgeSourceType ParseSourceType(string? value)
        => Enum.TryParse<KnowledgeSourceType>(value, true, out var parsed) ? parsed : KnowledgeSourceType.Internal;
}

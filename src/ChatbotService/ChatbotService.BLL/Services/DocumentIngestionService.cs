using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Options;
using ChatbotService.BLL.Security;
using ChatbotService.BLL.TextExtraction;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.Chatbot;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IKnowledgeDocumentRepository _documentRepository;
    private readonly ChunkingService _chunkingService;
    private readonly EmbeddingService _embeddingService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly InputSanitizer _sanitizer;
    private readonly EmbeddingOptions _embeddingOptions;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IKnowledgeDocumentRepository documentRepository,
        ChunkingService chunkingService,
        EmbeddingService embeddingService,
        ITextExtractionService textExtractionService,
        InputSanitizer sanitizer,
        IOptions<EmbeddingOptions> embeddingOptions,
        ILogger<DocumentIngestionService> logger)
    {
        _documentRepository = documentRepository;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _textExtractionService = textExtractionService;
        _sanitizer = sanitizer;
        _embeddingOptions = embeddingOptions.Value;
        _logger = logger;
    }

    public async Task<Guid> IngestAsync(IngestKnowledgeDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Document title is required.", nameof(request));
        }

        var extracted = _textExtractionService.Extract(request.Content, request.FileName, request.ContentType);
        var content = _sanitizer.Sanitize(extracted, 500_000);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Document content is required.", nameof(request));
        }

        var document = await _documentRepository.AddAsync(new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = _sanitizer.Sanitize(request.Title, 300),
            Content = content,
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
        var chunks = _chunkingService.Chunk(document.Content);
        if (chunks.Count == 0)
        {
            await _documentRepository.UpdateStatusAsync(document.Id, DocumentStatus.Failed, cancellationToken);
            return;
        }

        var embeddings = new List<EmbeddingVector>(chunks.Count);
        foreach (var chunk in chunks)
        {
            embeddings.Add(await _embeddingService.GenerateAsync(chunk, cancellationToken));
        }

        await _documentRepository.ReplaceChunksAsync(document.Id, chunks, _embeddingOptions.Model, embeddings, cancellationToken);
        _logger.LogInformation("Indexed chatbot document {DocumentId} with {ChunkCount} chunks", document.Id, chunks.Count);
    }

    private static KnowledgeSourceType ParseSourceType(string? value)
        => Enum.TryParse<KnowledgeSourceType>(value, true, out var parsed) ? parsed : KnowledgeSourceType.Internal;
}

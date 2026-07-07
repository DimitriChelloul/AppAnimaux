using ChatbotService.Domain.ValueObjects;
using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Services;

public sealed class CitationBuilder
{
    public IReadOnlyList<Citation> BuildCitations(IReadOnlyList<RagSearchResult> results)
        => results
            .GroupBy(result => result.ChunkId)
            .Select(group =>
            {
                var result = group.First();
                return new Citation
                {
                    DocumentId = result.DocumentId,
                    ChunkId = result.ChunkId,
                    Title = result.Title,
                    SourceUri = result.SourceUri,
                    Similarity = result.Similarity
                };
            })
            .ToArray();

    public IReadOnlyList<ChatbotSourceDto> ToDtos(IReadOnlyList<Citation> citations)
        => citations.Select(citation => new ChatbotSourceDto
        {
            DocumentId = citation.DocumentId,
            ChunkId = citation.ChunkId,
            Title = citation.Title,
            SourceUri = citation.SourceUri,
            Similarity = citation.Similarity
        }).ToArray();
}

using Shared.Contracts.Chatbot;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class CitationService
{
    public IReadOnlyList<ChatbotCitation> BuildCitations(IReadOnlyList<SemanticSearchResult> results)
        => results
            .GroupBy(result => result.ChunkId)
            .Select(group =>
            {
                var result = group.First();
                return new ChatbotCitation
                {
                    DocumentId = result.DocumentId,
                    ChunkId = result.ChunkId,
                    Title = result.Title,
                    SourceUri = result.SourceUri,
                    Similarity = result.Similarity
                };
            })
            .ToArray();

    public IReadOnlyList<ChatbotSourceDto> ToDtos(IReadOnlyList<ChatbotCitation> citations)
        => citations.Select(citation => new ChatbotSourceDto
        {
            DocumentId = citation.DocumentId,
            ChunkId = citation.ChunkId,
            Title = citation.Title,
            SourceUri = citation.SourceUri,
            Similarity = citation.Similarity
        }).ToArray();
}

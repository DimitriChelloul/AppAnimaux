using Shared.Semantic;

namespace ChatbotService.BLL.Abstractions;

public interface IRagRetriever
{
    Task<IReadOnlyList<SemanticSearchResult>> RetrieveAsync(string question, CancellationToken cancellationToken = default);
}

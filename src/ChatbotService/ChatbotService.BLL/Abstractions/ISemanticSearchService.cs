using Shared.Semantic;

namespace ChatbotService.BLL.Abstractions;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(string question, CancellationToken cancellationToken = default);
}

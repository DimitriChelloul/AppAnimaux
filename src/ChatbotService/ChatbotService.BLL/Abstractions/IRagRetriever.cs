using ChatbotService.Domain.ValueObjects;

namespace ChatbotService.BLL.Abstractions;

public interface IRagRetriever
{
    Task<IReadOnlyList<RagSearchResult>> RetrieveAsync(string question, CancellationToken cancellationToken = default);
}

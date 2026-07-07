using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class HallucinationGuard
{
    public string Apply(string answer, IReadOnlyList<SemanticSearchResult> sources)
    {
        if (sources.Count > 0 || answer.Contains("source", StringComparison.OrdinalIgnoreCase))
        {
            return answer;
        }

        return answer + "\n\nJe n'ai pas trouvé de source interne fiable à citer pour cette réponse.";
    }
}

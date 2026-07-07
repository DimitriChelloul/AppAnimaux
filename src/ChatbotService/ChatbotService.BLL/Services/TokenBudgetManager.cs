namespace ChatbotService.BLL.Services;

public sealed class TokenBudgetManager
{
    public int EstimateTokens(string text) => string.IsNullOrWhiteSpace(text) ? 0 : Math.Max(1, text.Length / 4);

    public IReadOnlyList<T> TakeWithinBudget<T>(IEnumerable<T> items, Func<T, string> textSelector, int maxTokens)
    {
        var selected = new List<T>();
        var used = 0;

        foreach (var item in items)
        {
            var estimated = EstimateTokens(textSelector(item));
            if (selected.Count > 0 && used + estimated > maxTokens)
            {
                break;
            }

            selected.Add(item);
            used += estimated;
        }

        return selected;
    }
}

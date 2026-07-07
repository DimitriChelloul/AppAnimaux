namespace ChatbotService.BLL.Security;

public sealed class PromptInjectionGuard
{
    private static readonly string[] SuspiciousTerms =
    [
        "ignore previous instructions",
        "ignore les instructions",
        "oublie les instructions",
        "system prompt",
        "developer message",
        "jailbreak",
        "révèle ta clé",
        "revele ta cle",
        "api key",
        "prompt secret"
    ];

    public bool IsSuspicious(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.ToLowerInvariant();
        return SuspiciousTerms.Any(normalized.Contains);
    }
}

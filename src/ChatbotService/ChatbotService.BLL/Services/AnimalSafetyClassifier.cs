namespace ChatbotService.BLL.Services;

public sealed class AnimalSafetyClassifier
{
    private static readonly string[] EmergencyTerms =
    [
        "urgence",
        "saignement",
        "sang",
        "intoxication",
        "empoisonnement",
        "respire mal",
        "détresse respiratoire",
        "detresse respiratoire",
        "paralysie",
        "paralysé",
        "paralyse",
        "convulsion",
        "convulsions",
        "douleur intense",
        "vomit beaucoup",
        "ne bouge plus",
        "inconscient",
        "comportement très anormal",
        "comportement tres anormal"
    ];

    public bool RequiresVeterinaryAttention(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = message.ToLowerInvariant();
        return EmergencyTerms.Any(normalized.Contains);
    }
}

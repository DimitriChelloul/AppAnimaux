namespace ChatbotService.BLL.Options;

public sealed class PromptOptions
{
    public string AssistantName { get; set; } = "AppAnimaux Assistant";
    public string Locale { get; set; } = "fr-FR";
    public string SystemInstructions { get; set; } = "Tu es AppAnimaux Assistant. Tu aides les proprietaires d'animaux et les utilisateurs d'AppAnimaux.";
    public int MaxPromptCharacters { get; set; } = 12000;
}

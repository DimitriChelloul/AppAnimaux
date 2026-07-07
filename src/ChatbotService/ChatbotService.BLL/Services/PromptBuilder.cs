using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class PromptBuilder
{
    public string BuildPrompt(
        string question,
        IReadOnlyList<ChatMessage> history,
        IReadOnlyList<SemanticSearchResult> context,
        bool requiresVeterinaryAttention)
    {
        var prompt = new System.Text.StringBuilder();

        prompt.AppendLine("Tu es le chatbot d'AppAnimaux, une application d'entraide entre propriétaires d'animaux.");
        prompt.AppendLine("Tu aides sur les questions générales concernant les animaux, la rédaction d'annonces de garde, promenade, visite ou covoiturage, et l'explication des fonctionnalités de l'application.");
        prompt.AppendLine();
        prompt.AppendLine("Regles de securite:");
        prompt.AppendLine("- Ne prétends jamais être vétérinaire.");
        prompt.AppendLine("- Ne pose jamais de diagnostic définitif.");
        prompt.AppendLine("- En cas d'urgence, symptôme grave ou doute sérieux, conseille de contacter rapidement un vétérinaire ou un service d'urgence vétérinaire.");
        prompt.AppendLine("- Réponds clairement et simplement.");
        prompt.AppendLine("- Cite les sources internes utilisées quand le contexte documentaire est utilisé.");
        prompt.AppendLine("- Si aucune source fiable n'est fournie, dis-le clairement.");

        if (requiresVeterinaryAttention)
        {
            prompt.AppendLine("- Cette question semble contenir un signal d'urgence ou de gravité: commence par recommander de contacter un vétérinaire rapidement.");
        }

        prompt.AppendLine();
        prompt.AppendLine("Contexte documentaire interne:");
        if (context.Count == 0)
        {
            prompt.AppendLine("Aucune source interne fiable n'a ete trouvee pour cette question.");
        }
        else
        {
            for (var i = 0; i < context.Count; i++)
            {
                var item = context[i];
                prompt.AppendLine($"[Source {i + 1}] {item.Title} (similarite {item.Similarity:0.00})");
                if (!string.IsNullOrWhiteSpace(item.SourceUri))
                {
                    prompt.AppendLine($"URI: {item.SourceUri}");
                }

                prompt.AppendLine(item.Content);
                prompt.AppendLine();
            }
        }

        prompt.AppendLine("Historique recent:");
        foreach (var message in history)
        {
            var role = message.Role == ChatRole.Assistant ? "Assistant" : "Utilisateur";
            prompt.AppendLine($"{role}: {message.Content}");
        }

        prompt.AppendLine();
        prompt.AppendLine($"Question utilisateur: {question}");
        prompt.AppendLine("Reponse:");

        return prompt.ToString();
    }
}

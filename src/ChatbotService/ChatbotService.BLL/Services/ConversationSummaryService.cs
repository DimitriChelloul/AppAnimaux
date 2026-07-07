using ChatbotService.BLL.Abstractions;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Enums;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class ConversationSummaryService : IConversationSummaryService
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly ILLMProvider _llmProvider;

    public ConversationSummaryService(IChatMessageRepository messageRepository, ILLMProvider llmProvider)
    {
        _messageRepository = messageRepository;
        _llmProvider = llmProvider;
    }

    public async Task<string> BuildSummaryAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var messages = await _messageRepository.GetAllAsync(conversationId, cancellationToken);
        var transcript = string.Join("\n", messages.Select(message => $"{Role(message.Role)}: {message.Content}"));
        var prompt = "Resume cette conversation AppAnimaux en francais, en gardant les faits utiles et les alertes veterinaire eventuelles.\n" + transcript;
        return await _llmProvider.GenerateAnswerAsync(prompt, cancellationToken);
    }

    private static string Role(ChatRole role) => role == ChatRole.Assistant ? "Assistant" : "Utilisateur";
}

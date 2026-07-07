using ChatbotService.BLL.Abstractions;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Shared.Contracts.Chatbot;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class ChatbotOrchestrator : IChatbotOrchestrator
{
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IRagRetriever _ragRetriever;
    private readonly ILLMProvider _llmProvider;
    private readonly PromptBuilder _promptBuilder;
    private readonly AnimalSafetyClassifier _safetyClassifier;
    private readonly CitationBuilder _citationBuilder;
    private readonly IConfiguration _configuration;

    public ChatbotOrchestrator(
        IChatConversationRepository conversationRepository,
        IChatMessageRepository messageRepository,
        IRagRetriever ragRetriever,
        ILLMProvider llmProvider,
        PromptBuilder promptBuilder,
        AnimalSafetyClassifier safetyClassifier,
        CitationBuilder citationBuilder,
        IConfiguration configuration)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _ragRetriever = ragRetriever;
        _llmProvider = llmProvider;
        _promptBuilder = promptBuilder;
        _safetyClassifier = safetyClassifier;
        _citationBuilder = citationBuilder;
        _configuration = configuration;
    }

    public async Task<AskChatbotResponse> AskAsync(AskChatbotRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        var conversation = request.ConversationId.HasValue
            ? await _conversationRepository.GetByIdAsync(request.ConversationId.Value, cancellationToken)
            : null;

        conversation ??= await _conversationRepository.CreateAsync(request.UserId, cancellationToken);

        var requiresVeterinaryAttention = _safetyClassifier.RequiresVeterinaryAttention(request.Message);

        await _messageRepository.AddAsync(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = ChatRole.User,
            Content = request.Message.Trim(),
            RequiresVeterinaryAttention = requiresVeterinaryAttention
        }, cancellationToken);

        var maxHistory = ConfigurationReader.GetInt(_configuration, "Chatbot:MaxConversationHistoryMessages", 8);
        var history = await _messageRepository.GetRecentAsync(conversation.Id, maxHistory, cancellationToken);
        var ragResults = await _ragRetriever.RetrieveAsync(request.Message, cancellationToken);
        var prompt = _promptBuilder.BuildPrompt(request.Message, history, ragResults, requiresVeterinaryAttention);
        var answer = await _llmProvider.GenerateAnswerAsync(prompt, cancellationToken);

        await _messageRepository.AddAsync(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = ChatRole.Assistant,
            Content = answer,
            RequiresVeterinaryAttention = requiresVeterinaryAttention
        }, cancellationToken);

        await _conversationRepository.TouchAsync(conversation.Id, cancellationToken);

        var citations = _citationBuilder.BuildCitations(ragResults);
        return new AskChatbotResponse
        {
            ConversationId = conversation.Id,
            Answer = answer,
            Sources = _citationBuilder.ToDtos(citations),
            RequiresVeterinaryAttention = requiresVeterinaryAttention
        };
    }
}

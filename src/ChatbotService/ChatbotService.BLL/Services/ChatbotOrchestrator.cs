using System.Diagnostics;
using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Observability;
using ChatbotService.BLL.Options;
using ChatbotService.BLL.Security;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.Chatbot;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class ChatbotOrchestrator : IChatbotOrchestrator
{
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IConversationMemoryService _memoryService;
    private readonly IRagRetriever _ragRetriever;
    private readonly ILLMProvider _llmProvider;
    private readonly PromptBuilder _promptBuilder;
    private readonly AnimalSafetyService _safetyService;
    private readonly CitationService _citationService;
    private readonly HallucinationGuard _hallucinationGuard;
    private readonly InputSanitizer _sanitizer;
    private readonly PromptInjectionGuard _promptInjectionGuard;
    private readonly ChatbotRuntimeOptions _options;
    private readonly ILogger<ChatbotOrchestrator> _logger;
    private readonly ChatbotMetrics _metrics;

    public ChatbotOrchestrator(
        IChatConversationRepository conversationRepository,
        IChatMessageRepository messageRepository,
        IConversationMemoryService memoryService,
        IRagRetriever ragRetriever,
        ILLMProvider llmProvider,
        PromptBuilder promptBuilder,
        AnimalSafetyService safetyService,
        CitationService citationService,
        HallucinationGuard hallucinationGuard,
        InputSanitizer sanitizer,
        PromptInjectionGuard promptInjectionGuard,
        IOptions<ChatbotRuntimeOptions> options,
        ILogger<ChatbotOrchestrator> logger,
        ChatbotMetrics metrics)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _memoryService = memoryService;
        _ragRetriever = ragRetriever;
        _llmProvider = llmProvider;
        _promptBuilder = promptBuilder;
        _safetyService = safetyService;
        _citationService = citationService;
        _hallucinationGuard = hallucinationGuard;
        _sanitizer = sanitizer;
        _promptInjectionGuard = promptInjectionGuard;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<AskChatbotResponse> AskAsync(AskChatbotRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var message = _sanitizer.Sanitize(request.Message, _options.MaxUserMessageCharacters);
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        var conversation = request.ConversationId.HasValue
            ? await _conversationRepository.GetByIdAsync(request.ConversationId.Value, cancellationToken)
            : null;
        conversation ??= await _conversationRepository.CreateAsync(request.UserId, cancellationToken);

        var requiresVeterinaryAttention = _safetyService.RequiresVeterinaryAttention(message);
        var promptInjectionSuspected = _promptInjectionGuard.IsSuspicious(message);
        _metrics.Request(requiresVeterinaryAttention);

        await _messageRepository.AddAsync(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = ChatRole.User,
            Content = message,
            RequiresVeterinaryAttention = requiresVeterinaryAttention
        }, cancellationToken);

        var memory = await _memoryService.GetMemoryAsync(conversation.Id, cancellationToken);
        var ragResults = await _ragRetriever.RetrieveAsync(message, cancellationToken);
        var prompt = _promptBuilder.BuildPrompt(message, memory, ragResults, requiresVeterinaryAttention, promptInjectionSuspected);
        var rawAnswer = await _llmProvider.GenerateAnswerAsync(prompt, cancellationToken);
        var answer = _hallucinationGuard.Apply(rawAnswer, ragResults);

        await _messageRepository.AddAsync(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = ChatRole.Assistant,
            Content = answer,
            RequiresVeterinaryAttention = requiresVeterinaryAttention
        }, cancellationToken);

        await _conversationRepository.TouchAsync(conversation.Id, cancellationToken);
        await _memoryService.SummarizeIfNeededAsync(conversation.Id, cancellationToken);

        _logger.LogInformation(
            "Chatbot answered conversation {ConversationId} in {ElapsedMs} ms with {SourceCount} sources and emergency={Emergency}",
            conversation.Id,
            stopwatch.ElapsedMilliseconds,
            ragResults.Count,
            requiresVeterinaryAttention);
        _metrics.ResponseTime(stopwatch.Elapsed.TotalMilliseconds);

        var citations = _citationService.BuildCitations(ragResults);
        return new AskChatbotResponse
        {
            ConversationId = conversation.Id,
            Answer = answer,
            Sources = _citationService.ToDtos(citations),
            RequiresVeterinaryAttention = requiresVeterinaryAttention
        };
    }
}

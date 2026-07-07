using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Models;
using ChatbotService.BLL.Options;
using ChatbotService.DAL.Abstractions;
using Microsoft.Extensions.Options;

namespace ChatbotService.BLL.Services;

public sealed class ConversationMemoryService : IConversationMemoryService
{
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IConversationSummaryService _summaryService;
    private readonly ChatbotRuntimeOptions _options;

    public ConversationMemoryService(IChatConversationRepository conversationRepository, IChatMessageRepository messageRepository, IConversationSummaryService summaryService, IOptions<ChatbotRuntimeOptions> options)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _summaryService = summaryService;
        _options = options.Value;
    }

    public async Task<ConversationMemory> GetMemoryAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var summary = await _conversationRepository.GetSummaryAsync(conversationId, cancellationToken);
        var recent = await _messageRepository.GetRecentAsync(conversationId, _options.MaxConversationHistoryMessages, cancellationToken);
        return new ConversationMemory(recent, summary?.Summary);
    }

    public async Task SummarizeIfNeededAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var count = await _messageRepository.CountByConversationAsync(conversationId, cancellationToken);
        if (count < _options.MaxStoredMessagesBeforeSummary)
        {
            return;
        }

        var summary = await _summaryService.BuildSummaryAsync(conversationId, cancellationToken);
        await _conversationRepository.UpsertSummaryAsync(new Domain.Entities.ConversationSummary
        {
            ConversationId = conversationId,
            Summary = summary,
            CoveredMessageCount = count
        }, cancellationToken);
        await _messageRepository.DeleteOlderThanLatestAsync(conversationId, _options.MaxConversationHistoryMessages, cancellationToken);
    }
}

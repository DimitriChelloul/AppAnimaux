using ChatbotService.BLL.Abstractions;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Services;

public sealed class ConversationQueryService : IConversationQueryService
{
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;

    public ConversationQueryService(IChatConversationRepository conversationRepository, IChatMessageRepository messageRepository)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<ChatbotConversationDto?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        var summary = await _conversationRepository.GetSummaryAsync(conversationId, cancellationToken);
        var messages = await _messageRepository.GetAllAsync(conversationId, cancellationToken);
        return new ChatbotConversationDto
        {
            Id = conversation.Id,
            UserId = conversation.UserId,
            Title = conversation.Title,
            Summary = summary?.Summary,
            Messages = messages.Select(ToDto).ToArray(),
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    private static ChatbotMessageDto ToDto(ChatMessage message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        Role = message.Role.ToString().ToLowerInvariant(),
        Content = message.Content,
        RequiresVeterinaryAttention = message.RequiresVeterinaryAttention,
        CreatedAt = message.CreatedAt
    };
}

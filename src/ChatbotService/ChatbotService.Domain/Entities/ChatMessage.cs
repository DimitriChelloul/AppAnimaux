using ChatbotService.Domain.Enums;

namespace ChatbotService.Domain.Entities;

public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
    public bool RequiresVeterinaryAttention { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

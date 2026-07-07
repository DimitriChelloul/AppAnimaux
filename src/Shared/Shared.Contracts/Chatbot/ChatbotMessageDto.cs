namespace Shared.Contracts.Chatbot;

public sealed record ChatbotMessageDto
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public string Role { get; init; } = "user";
    public string Content { get; init; } = "";
    public bool RequiresVeterinaryAttention { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

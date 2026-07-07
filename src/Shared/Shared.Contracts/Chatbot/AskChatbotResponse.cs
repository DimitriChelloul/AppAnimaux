namespace Shared.Contracts.Chatbot;

public sealed record AskChatbotResponse
{
    public Guid ConversationId { get; init; }
    public string Answer { get; init; } = "";
    public IReadOnlyList<ChatbotSourceDto> Sources { get; init; } = Array.Empty<ChatbotSourceDto>();
    public bool RequiresVeterinaryAttention { get; init; }
}

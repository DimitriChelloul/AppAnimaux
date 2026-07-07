namespace Shared.Contracts.Chatbot;

public sealed record AskChatbotRequest
{
    public Guid? ConversationId { get; init; }
    public Guid? UserId { get; init; }
    public string Message { get; init; } = "";
}

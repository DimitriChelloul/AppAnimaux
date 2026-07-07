namespace Shared.Contracts.Chatbot;

public sealed record ChatbotFeedbackRequest
{
    public Guid ConversationId { get; init; }
    public Guid? MessageId { get; init; }
    public Guid? UserId { get; init; }
    public int? Rating { get; init; }
    public string? Comment { get; init; }
}

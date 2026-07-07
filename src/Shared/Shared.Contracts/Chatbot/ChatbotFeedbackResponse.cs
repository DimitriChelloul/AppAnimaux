namespace Shared.Contracts.Chatbot;

public sealed record ChatbotFeedbackResponse
{
    public Guid FeedbackId { get; init; }
}

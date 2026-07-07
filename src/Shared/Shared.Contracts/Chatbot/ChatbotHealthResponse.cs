namespace Shared.Contracts.Chatbot;

public sealed record ChatbotHealthResponse
{
    public string Status { get; init; } = "ok";
    public DateTimeOffset CheckedAt { get; init; }
}

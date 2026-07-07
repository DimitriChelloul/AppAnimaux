namespace Shared.Contracts.Chatbot;

public sealed record ChatbotReindexResponse
{
    public int IndexedDocuments { get; init; }
}

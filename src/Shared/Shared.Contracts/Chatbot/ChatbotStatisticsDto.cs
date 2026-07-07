namespace Shared.Contracts.Chatbot;

public sealed record ChatbotStatisticsDto
{
    public int DocumentCount { get; init; }
    public int IndexedDocumentCount { get; init; }
    public int ChunkCount { get; init; }
    public int EmbeddingCount { get; init; }
    public int ConversationCount { get; init; }
    public int MessageCount { get; init; }
}

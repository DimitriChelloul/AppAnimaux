namespace ChatbotService.Api.Options;

public sealed class ChatbotOptions
{
    public int MaxRetrievedChunks { get; set; } = 5;
    public double MinSimilarity { get; set; } = 0.70;
    public int MaxConversationHistoryMessages { get; set; } = 8;
    public int ChunkSize { get; set; } = 900;
    public int ChunkOverlap { get; set; } = 150;
}

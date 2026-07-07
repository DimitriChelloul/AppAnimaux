namespace ChatbotService.BLL.Options;

public sealed class ChatbotRuntimeOptions
{
    public int MaxConversationHistoryMessages { get; set; } = 10;
    public int MaxStoredMessagesBeforeSummary { get; set; } = 24;
    public int ChunkSize { get; set; } = 900;
    public int ChunkOverlap { get; set; } = 150;
    public int MaxUserMessageCharacters { get; set; } = 4000;
}

namespace ChatbotService.BLL.Options;

public sealed class EmbeddingOptions
{
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = "text-embedding-3-small";
    public bool CacheEnabled { get; set; } = true;
}

namespace ChatbotService.Domain.Entities;

public sealed class EmbeddingCacheEntry
{
    public string CacheKey { get; set; } = "";
    public string InputHash { get; set; } = "";
    public string Model { get; set; } = "";
    public int Dimensions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

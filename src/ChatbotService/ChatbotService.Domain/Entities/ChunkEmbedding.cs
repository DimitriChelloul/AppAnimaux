namespace ChatbotService.Domain.Entities;

public sealed class ChunkEmbedding
{
    public Guid ChunkId { get; set; }
    public string Model { get; set; } = "";
    public int Dimensions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

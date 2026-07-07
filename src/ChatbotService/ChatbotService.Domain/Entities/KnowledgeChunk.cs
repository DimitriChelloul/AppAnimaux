namespace ChatbotService.Domain.Entities;

public sealed class KnowledgeChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
    public int TokenEstimate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

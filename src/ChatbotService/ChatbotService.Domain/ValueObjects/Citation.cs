namespace ChatbotService.Domain.ValueObjects;

public sealed record Citation
{
    public Guid DocumentId { get; init; }
    public Guid ChunkId { get; init; }
    public string Title { get; init; } = "";
    public string? SourceUri { get; init; }
    public double Similarity { get; init; }
}

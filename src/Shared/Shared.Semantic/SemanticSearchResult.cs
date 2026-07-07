namespace Shared.Semantic;

public sealed record SemanticSearchResult
{
    public Guid ChunkId { get; init; }
    public Guid DocumentId { get; init; }
    public string Content { get; init; } = "";
    public string Title { get; init; } = "";
    public string? SourceUri { get; init; }
    public double Similarity { get; init; }
    public int ChunkIndex { get; init; }
}

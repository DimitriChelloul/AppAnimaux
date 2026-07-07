namespace ChatbotService.Domain.ValueObjects;

public sealed record Citation(Guid DocumentId, Guid ChunkId, string Title, string? SourceUri, double Similarity);

namespace Shared.Contracts.Chatbot;

public sealed record ChatbotDocumentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string SourceType { get; init; } = "internal";
    public string? SourceUri { get; init; }
    public string? Locale { get; init; }
    public string Status { get; init; } = "draft";
    public int ChunkCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

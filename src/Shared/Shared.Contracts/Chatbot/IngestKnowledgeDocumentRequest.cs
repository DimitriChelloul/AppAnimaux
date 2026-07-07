namespace Shared.Contracts.Chatbot;

public sealed record IngestKnowledgeDocumentRequest
{
    public string Title { get; init; } = "";
    public string Content { get; init; } = "";
    public string SourceType { get; init; } = "internal";
    public string? SourceUri { get; init; }
    public string? Locale { get; init; } = "fr-FR";
}

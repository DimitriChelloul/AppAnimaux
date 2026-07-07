namespace Shared.Contracts.Chatbot;

public sealed record UploadKnowledgeDocumentRequest
{
    public string Title { get; init; } = "";
    public string Content { get; init; } = "";
    public string FileName { get; init; } = "document.txt";
    public string? ContentType { get; init; }
    public string SourceType { get; init; } = "internal";
    public string? SourceUri { get; init; }
    public string? Locale { get; init; } = "fr-FR";
}

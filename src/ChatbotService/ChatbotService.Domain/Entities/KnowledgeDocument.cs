using ChatbotService.Domain.Enums;

namespace ChatbotService.Domain.Entities;

public sealed class KnowledgeDocument
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public KnowledgeSourceType SourceType { get; set; }
    public string? SourceUri { get; set; }
    public string? Locale { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

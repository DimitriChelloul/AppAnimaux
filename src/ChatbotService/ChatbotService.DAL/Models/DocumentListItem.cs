namespace ChatbotService.DAL.Models;

public sealed record DocumentListItem(
    Guid Id,
    string Title,
    string SourceType,
    string? SourceUri,
    string? Locale,
    string Status,
    int ChunkCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

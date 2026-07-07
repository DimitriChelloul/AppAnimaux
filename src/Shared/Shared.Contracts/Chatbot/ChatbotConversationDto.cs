namespace Shared.Contracts.Chatbot;

public sealed record ChatbotConversationDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<ChatbotMessageDto> Messages { get; init; } = Array.Empty<ChatbotMessageDto>();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

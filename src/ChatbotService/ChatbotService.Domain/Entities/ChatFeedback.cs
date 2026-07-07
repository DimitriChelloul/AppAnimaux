namespace ChatbotService.Domain.Entities;

public sealed class ChatFeedback
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? UserId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

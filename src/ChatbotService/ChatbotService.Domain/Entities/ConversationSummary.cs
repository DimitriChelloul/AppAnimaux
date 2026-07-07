namespace ChatbotService.Domain.Entities;

public sealed class ConversationSummary
{
    public Guid ConversationId { get; set; }
    public string Summary { get; set; } = "";
    public int CoveredMessageCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

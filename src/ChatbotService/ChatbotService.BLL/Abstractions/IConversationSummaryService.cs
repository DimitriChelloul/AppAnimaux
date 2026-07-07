namespace ChatbotService.BLL.Abstractions;

public interface IConversationSummaryService
{
    Task<string> BuildSummaryAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

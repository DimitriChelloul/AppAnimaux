namespace ChatbotService.Domain.Events;

public sealed record ChatbotQuestionAskedEvent(Guid ConversationId, Guid? UserId, bool RequiresVeterinaryAttention, DateTimeOffset OccurredAt);

using PrivateMessagingService.Domain.Entities;

namespace PrivateMessagingService.DAL.Repositories;

public interface IConversationRepository
{
    Task<Guid> CreateAsync(Guid createdByUserId, string type, string? title, IReadOnlyCollection<Guid> memberIds, CancellationToken ct);
    Task<Conversation?> GetByIdForUserAsync(Guid conversationId, Guid userId, CancellationToken ct);
    Task<IReadOnlyCollection<Conversation>> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid conversationId, CancellationToken ct);
    Task<IReadOnlyCollection<Message>> GetMessagesAsync(Guid conversationId, Guid userId, int page, int pageSize, CancellationToken ct);
    Task<Message?> AddMessageAsync(Guid conversationId, Guid senderUserId, string messageType, string? content, string? attachmentsJson, CancellationToken ct);
    Task<bool> MarkReadAsync(Guid conversationId, Guid userId, Guid? messageId, CancellationToken ct);
}

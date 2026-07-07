using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using Dapper;
using Shared.Persistence.Abstractions;

namespace ChatbotService.DAL.Repositories;

public sealed class ChatConversationRepository : IChatConversationRepository
{
    private readonly IDbConnectionFactory _db;

    public ChatConversationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ChatConversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<ChatConversation>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                title AS Title,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM chatbot_conversations
            WHERE id = @ConversationId
            """,
            new { ConversationId = conversationId });
    }

    public async Task<ChatConversation> CreateAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<ChatConversation>(
            """
            INSERT INTO chatbot_conversations (id, user_id, created_at, updated_at)
            VALUES (@Id, @UserId, now(), now())
            RETURNING
                id AS Id,
                user_id AS UserId,
                title AS Title,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            new { Id = Guid.NewGuid(), UserId = userId });
    }

    public async Task TouchAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            "UPDATE chatbot_conversations SET updated_at = now() WHERE id = @ConversationId",
            new { ConversationId = conversationId });
    }
}

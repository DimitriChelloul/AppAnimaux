using Dapper;
using PrivateMessagingService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace PrivateMessagingService.DAL.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly IDbConnectionFactory _db;

    public ConversationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> CreateAsync(Guid createdByUserId, string type, string? title, IReadOnlyCollection<Guid> memberIds, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        var conversationId = await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO conversations (created_by_user_id, type, title)
            VALUES (@CreatedByUserId, @Type, @Title)
            RETURNING id
            """,
            new { CreatedByUserId = createdByUserId, Type = type, Title = title },
            tx);

        foreach (var memberId in memberIds.Distinct())
        {
            await cn.ExecuteAsync(
                """
                INSERT INTO conversation_members (conversation_id, user_id, role)
                VALUES (@ConversationId, @UserId, @Role)
                ON CONFLICT (conversation_id, user_id) DO NOTHING
                """,
                new { ConversationId = conversationId, UserId = memberId, Role = memberId == createdByUserId ? "owner" : "member" },
                tx);
        }

        tx.Commit();
        return conversationId;
    }

    public async Task<Conversation?> GetByIdForUserAsync(Guid conversationId, Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<Conversation>(
            $"""
            {SelectConversationSql}
            INNER JOIN conversation_members cm ON cm.conversation_id = c.id
            WHERE c.id = @ConversationId
              AND cm.user_id = @UserId
              AND cm.is_hidden = false
            """,
            new { ConversationId = conversationId, UserId = userId });
    }

    public async Task<IReadOnlyCollection<Conversation>> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await cn.QueryAsync<Conversation>(
            $"""
            {SelectConversationSql}
            INNER JOIN conversation_members cm ON cm.conversation_id = c.id
            WHERE cm.user_id = @UserId
              AND cm.is_hidden = false
            ORDER BY c.last_message_at DESC NULLS LAST, c.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { UserId = userId, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid conversationId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<Guid>(
            "SELECT user_id FROM conversation_members WHERE conversation_id = @ConversationId AND is_hidden = false",
            new { ConversationId = conversationId });

        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<Message>> GetMessagesAsync(Guid conversationId, Guid userId, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await cn.QueryAsync<Message>(
            """
            SELECT m.id AS Id, m.conversation_id AS ConversationId, m.sender_user_id AS SenderUserId,
                   m.message_type AS MessageType, m.content AS Content, m.attachments::text AS Attachments,
                   m.is_deleted AS IsDeleted, m.created_at AS CreatedAt, m.edited_at AS EditedAt
            FROM messages m
            INNER JOIN conversation_members cm ON cm.conversation_id = m.conversation_id
            WHERE m.conversation_id = @ConversationId
              AND cm.user_id = @UserId
              AND m.is_deleted = false
            ORDER BY m.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { ConversationId = conversationId, UserId = userId, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task<Message?> AddMessageAsync(Guid conversationId, Guid senderUserId, string messageType, string? content, string? attachmentsJson, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        var isMember = await cn.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM conversation_members
                WHERE conversation_id = @ConversationId
                  AND user_id = @SenderUserId
                  AND is_hidden = false
            )
            """,
            new { ConversationId = conversationId, SenderUserId = senderUserId },
            tx);
        if (!isMember)
        {
            tx.Rollback();
            return null;
        }

        var message = await cn.QuerySingleAsync<Message>(
            """
            INSERT INTO messages (conversation_id, sender_user_id, message_type, content, attachments)
            VALUES (@ConversationId, @SenderUserId, @MessageType, @Content, CAST(@Attachments AS jsonb))
            RETURNING id AS Id, conversation_id AS ConversationId, sender_user_id AS SenderUserId,
                      message_type AS MessageType, content AS Content, attachments::text AS Attachments,
                      is_deleted AS IsDeleted, created_at AS CreatedAt, edited_at AS EditedAt
            """,
            new { ConversationId = conversationId, SenderUserId = senderUserId, MessageType = messageType, Content = content, Attachments = attachmentsJson ?? "[]" },
            tx);

        await cn.ExecuteAsync(
            """
            UPDATE conversations
            SET last_message_id = @MessageId,
                last_message_at = @CreatedAt,
                updated_at = now()
            WHERE id = @ConversationId
            """,
            new { MessageId = message.Id, CreatedAt = message.CreatedAt, ConversationId = conversationId },
            tx);

        tx.Commit();
        return message;
    }

    public async Task<bool> MarkReadAsync(Guid conversationId, Guid userId, Guid? messageId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE conversation_members
            SET last_read_message_id = COALESCE(@MessageId, last_read_message_id),
                last_read_at = now()
            WHERE conversation_id = @ConversationId
              AND user_id = @UserId
            """,
            new { ConversationId = conversationId, UserId = userId, MessageId = messageId });

        return rows > 0;
    }

    private const string SelectConversationSql =
        """
        SELECT c.id AS Id, c.type AS Type, c.title AS Title, c.created_by_user_id AS CreatedByUserId,
               c.last_message_at AS LastMessageAt, c.last_message_id AS LastMessageId,
               c.is_archived AS IsArchived, c.created_at AS CreatedAt, c.updated_at AS UpdatedAt
        FROM conversations c
        """;
}

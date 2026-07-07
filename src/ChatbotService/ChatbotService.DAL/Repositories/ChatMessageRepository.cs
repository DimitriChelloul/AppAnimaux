using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using Dapper;
using Shared.Persistence.Abstractions;

namespace ChatbotService.DAL.Repositories;

public sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly IDbConnectionFactory _db;

    public ChatMessageRepository(IDbConnectionFactory db) => _db = db;

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            INSERT INTO chatbot_messages (id, conversation_id, role, content, requires_veterinary_attention, created_at)
            VALUES (@Id, @ConversationId, @Role, @Content, @RequiresVeterinaryAttention, now())
            """,
            new
            {
                Id = message.Id == Guid.Empty ? Guid.NewGuid() : message.Id,
                message.ConversationId,
                Role = message.Role.ToString().ToLowerInvariant(),
                message.Content,
                message.RequiresVeterinaryAttention
            });
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(Guid conversationId, int limit, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<ChatMessageRow>(
            """
            SELECT id AS Id, conversation_id AS ConversationId, role AS Role, content AS Content,
                   requires_veterinary_attention AS RequiresVeterinaryAttention, created_at AS CreatedAt
            FROM chatbot_messages
            WHERE conversation_id = @ConversationId
            ORDER BY created_at DESC
            LIMIT @Limit
            """,
            new { ConversationId = conversationId, Limit = Math.Max(1, limit) });

        return rows.Reverse().Select(ToMessage).ToArray();
    }

    public async Task<IReadOnlyList<ChatMessage>> GetAllAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<ChatMessageRow>(
            """
            SELECT id AS Id, conversation_id AS ConversationId, role AS Role, content AS Content,
                   requires_veterinary_attention AS RequiresVeterinaryAttention, created_at AS CreatedAt
            FROM chatbot_messages
            WHERE conversation_id = @ConversationId
            ORDER BY created_at ASC
            """,
            new { ConversationId = conversationId });

        return rows.Select(ToMessage).ToArray();
    }

    public async Task<int> CountByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();
        return await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_messages WHERE conversation_id = @ConversationId", new { ConversationId = conversationId });
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();
        return await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM chatbot_messages");
    }

    public async Task DeleteOlderThanLatestAsync(Guid conversationId, int keepLatest, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            DELETE FROM chatbot_messages
            WHERE conversation_id = @ConversationId
              AND id NOT IN (
                  SELECT id FROM chatbot_messages
                  WHERE conversation_id = @ConversationId
                  ORDER BY created_at DESC
                  LIMIT @KeepLatest
              )
            """,
            new { ConversationId = conversationId, KeepLatest = Math.Max(1, keepLatest) });
    }

    private static ChatMessage ToMessage(ChatMessageRow row) => new()
    {
        Id = row.Id,
        ConversationId = row.ConversationId,
        Role = Enum.TryParse<ChatRole>(row.Role, true, out var parsed) ? parsed : ChatRole.User,
        Content = row.Content,
        RequiresVeterinaryAttention = row.RequiresVeterinaryAttention,
        CreatedAt = row.CreatedAt
    };

    private sealed record ChatMessageRow(Guid Id, Guid ConversationId, string Role, string Content, bool RequiresVeterinaryAttention, DateTimeOffset CreatedAt);
}

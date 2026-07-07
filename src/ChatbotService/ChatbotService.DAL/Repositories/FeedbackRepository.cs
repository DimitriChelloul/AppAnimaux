using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using Dapper;
using Shared.Persistence.Abstractions;

namespace ChatbotService.DAL.Repositories;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly IDbConnectionFactory _db;

    public FeedbackRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> AddAsync(ChatFeedback feedback, CancellationToken cancellationToken = default)
    {
        using var cn = _db.Create();
        cn.Open();

        var id = feedback.Id == Guid.Empty ? Guid.NewGuid() : feedback.Id;
        await cn.ExecuteAsync(
            """
            INSERT INTO chatbot_feedback (id, conversation_id, message_id, user_id, rating, comment, created_at)
            VALUES (@Id, @ConversationId, @MessageId, @UserId, @Rating, @Comment, now())
            """,
            new { Id = id, feedback.ConversationId, feedback.MessageId, feedback.UserId, feedback.Rating, feedback.Comment });

        return id;
    }
}

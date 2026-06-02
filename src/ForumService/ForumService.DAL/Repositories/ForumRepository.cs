using Dapper;
using ForumService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace ForumService.DAL.Repositories;

public sealed class ForumRepository : IForumRepository
{
    private readonly IDbConnectionFactory _db;

    public ForumRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ForumCategory> CreateCategoryAsync(ForumCategory category, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<ForumCategory>(
            """
            INSERT INTO forum_categories (id, name, description, slug, is_locked, sort_order)
            VALUES (@Id, @Name, @Description, @Slug, @IsLocked, @SortOrder)
            RETURNING
                id AS Id,
                name AS Name,
                description AS Description,
                slug AS Slug,
                is_locked AS IsLocked,
                sort_order AS SortOrder,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            category);
    }

    public async Task<IReadOnlyCollection<ForumCategory>> GetCategoriesAsync(CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<ForumCategory>(
            """
            SELECT
                id AS Id,
                name AS Name,
                description AS Description,
                slug AS Slug,
                is_locked AS IsLocked,
                sort_order AS SortOrder,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM forum_categories
            ORDER BY sort_order, name
            """);

        return rows.ToArray();
    }

    public async Task<ForumCategory?> GetCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<ForumCategory>(
            """
            SELECT
                id AS Id,
                name AS Name,
                description AS Description,
                slug AS Slug,
                is_locked AS IsLocked,
                sort_order AS SortOrder,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM forum_categories
            WHERE id = @CategoryId
            """,
            new { CategoryId = categoryId });
    }

    public async Task<ForumTopic> CreateTopicAsync(ForumTopic topic, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<ForumTopic>(
            """
            INSERT INTO forum_topics (
                id, category_id, author_user_id, title, slug, content,
                attachments, tags, last_post_at
            )
            VALUES (
                @Id, @CategoryId, @AuthorUserId, @Title, @Slug, @Content,
                CAST(@AttachmentsJson AS jsonb), @Tags, now()
            )
            RETURNING
                id AS Id,
                category_id AS CategoryId,
                author_user_id AS AuthorUserId,
                title AS Title,
                slug AS Slug,
                content AS Content,
                attachments::text AS AttachmentsJson,
                tags AS Tags,
                status AS Status,
                is_pinned AS IsPinned,
                views_count AS ViewsCount,
                replies_count AS RepliesCount,
                last_post_at AS LastPostAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            topic);
    }

    public async Task<ForumTopic?> GetTopicAsync(Guid topicId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<ForumTopic>(
            $"""
            {SelectTopicSql}
            WHERE id = @TopicId
            """,
            new { TopicId = topicId });
    }

    public async Task<IReadOnlyCollection<ForumTopic>> GetTopicsAsync(Guid? categoryId, string? status, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<ForumTopic>(
            $"""
            {SelectTopicSql}
            WHERE (@CategoryId IS NULL OR category_id = @CategoryId)
              AND (@Status IS NULL OR status = @Status)
            ORDER BY is_pinned DESC, COALESCE(last_post_at, created_at) DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { CategoryId = categoryId, Status = status, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task IncrementTopicViewsAsync(Guid topicId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync("UPDATE forum_topics SET views_count = views_count + 1 WHERE id = @TopicId", new { TopicId = topicId });
    }

    public async Task<ForumPost> CreatePostAsync(ForumPost post, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        var created = await cn.QuerySingleAsync<ForumPost>(
            """
            INSERT INTO forum_posts (id, topic_id, author_user_id, content, attachments)
            VALUES (@Id, @TopicId, @AuthorUserId, @Content, CAST(@AttachmentsJson AS jsonb))
            RETURNING
                id AS Id,
                topic_id AS TopicId,
                author_user_id AS AuthorUserId,
                content AS Content,
                attachments::text AS AttachmentsJson,
                status AS Status,
                edited_at AS EditedAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            post,
            tx);

        await cn.ExecuteAsync(
            """
            UPDATE forum_topics
            SET replies_count = replies_count + 1,
                last_post_at = now(),
                updated_at = now()
            WHERE id = @TopicId
            """,
            new { post.TopicId },
            tx);

        tx.Commit();
        return created;
    }

    public async Task<IReadOnlyCollection<ForumPost>> GetPostsAsync(Guid topicId, bool includeHidden, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<ForumPost>(
            """
            SELECT
                id AS Id,
                topic_id AS TopicId,
                author_user_id AS AuthorUserId,
                content AS Content,
                attachments::text AS AttachmentsJson,
                status AS Status,
                edited_at AS EditedAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM forum_posts
            WHERE topic_id = @TopicId
              AND (@IncludeHidden OR status = 'published')
            ORDER BY created_at
            LIMIT @PageSize OFFSET @Offset
            """,
            new { TopicId = topicId, IncludeHidden = includeHidden, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task<ForumPost?> GetPostAsync(Guid postId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<ForumPost>(
            """
            SELECT
                id AS Id,
                topic_id AS TopicId,
                author_user_id AS AuthorUserId,
                content AS Content,
                attachments::text AS AttachmentsJson,
                status AS Status,
                edited_at AS EditedAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM forum_posts
            WHERE id = @PostId
            """,
            new { PostId = postId });
    }

    public async Task<bool> UpdatePostAsync(Guid postId, Guid authorUserId, string content, string? attachmentsJson, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE forum_posts
            SET content = @Content,
                attachments = CAST(@AttachmentsJson AS jsonb),
                edited_at = now(),
                updated_at = now()
            WHERE id = @PostId
              AND author_user_id = @AuthorUserId
              AND status = 'published'
            """,
            new { PostId = postId, AuthorUserId = authorUserId, Content = content, AttachmentsJson = attachmentsJson });

        return rows > 0;
    }

    public async Task<bool> DeletePostAsync(Guid postId, Guid authorUserId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE forum_posts
            SET status = 'deleted',
                updated_at = now()
            WHERE id = @PostId
              AND author_user_id = @AuthorUserId
              AND status <> 'deleted'
            """,
            new { PostId = postId, AuthorUserId = authorUserId });

        return rows > 0;
    }

    public async Task<ForumFlag> FlagAsync(Guid flaggedByUserId, string targetType, Guid targetId, string reason, string? details, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<ForumFlag>(
            """
            INSERT INTO forum_flags (flagged_by_user_id, target_type, target_id, reason, details)
            VALUES (@FlaggedByUserId, @TargetType, @TargetId, @Reason, @Details)
            ON CONFLICT (flagged_by_user_id, target_type, target_id)
            DO UPDATE SET
                reason = EXCLUDED.reason,
                details = EXCLUDED.details,
                status = 'open'
            RETURNING
                id AS Id,
                flagged_by_user_id AS FlaggedByUserId,
                target_type AS TargetType,
                target_id AS TargetId,
                reason AS Reason,
                details AS Details,
                status AS Status,
                reviewed_by AS ReviewedBy,
                reviewed_at AS ReviewedAt,
                decision_notes AS DecisionNotes,
                created_at AS CreatedAt
            """,
            new { FlaggedByUserId = flaggedByUserId, TargetType = targetType, TargetId = targetId, Reason = reason, Details = details });
    }

    public async Task<bool> ModerateAsync(string targetType, Guid targetId, string status, Guid reviewedBy, string? reason, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var table = targetType == "topic" ? "forum_topics" : "forum_posts";
        var rows = await cn.ExecuteAsync(
            $"""
            UPDATE {table}
            SET status = @Status,
                updated_at = now()
            WHERE id = @TargetId
            """,
            new { TargetId = targetId, Status = status });

        if (rows > 0)
        {
            await cn.ExecuteAsync(
                """
                UPDATE forum_flags
                SET status = 'reviewed',
                    reviewed_by = @ReviewedBy,
                    reviewed_at = now(),
                    decision_notes = @Reason
                WHERE target_type = @TargetType
                  AND target_id = @TargetId
                  AND status = 'open'
                """,
                new { TargetType = targetType, TargetId = targetId, ReviewedBy = reviewedBy, Reason = reason });
        }

        return rows > 0;
    }

    private const string SelectTopicSql =
        """
        SELECT
            id AS Id,
            category_id AS CategoryId,
            author_user_id AS AuthorUserId,
            title AS Title,
            slug AS Slug,
            content AS Content,
            attachments::text AS AttachmentsJson,
            tags AS Tags,
            status AS Status,
            is_pinned AS IsPinned,
            views_count AS ViewsCount,
            replies_count AS RepliesCount,
            last_post_at AS LastPostAt,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM forum_topics
        """;
}

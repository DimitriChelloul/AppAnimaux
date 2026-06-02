using Dapper;
using ReviewService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace ReviewService.DAL.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly IDbConnectionFactory _db;

    public ReviewRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Review> CreateAsync(Review review, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Review>(
            """
            INSERT INTO reviews (
                id, help_match_id, reviewer_user_id, reviewee_user_id,
                pet_id, rating, comment, is_public
            )
            VALUES (
                @Id, @HelpMatchId, @ReviewerUserId, @RevieweeUserId,
                @PetId, @Rating, @Comment, @IsPublic
            )
            RETURNING
                id AS Id,
                help_match_id AS HelpMatchId,
                reviewer_user_id AS ReviewerUserId,
                reviewee_user_id AS RevieweeUserId,
                pet_id AS PetId,
                rating AS Rating,
                comment AS Comment,
                is_public AS IsPublic,
                moderation_status AS ModerationStatus,
                moderated_by AS ModeratedBy,
                moderated_at AS ModeratedAt,
                moderation_reason AS ModerationReason,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            review);
    }

    public Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken ct)
    {
        return GetSingleAsync("id = @ReviewId", new { ReviewId = reviewId });
    }

    public async Task<IReadOnlyCollection<Review>> GetByRevieweeAsync(Guid revieweeUserId, bool includeHidden, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var reviews = await cn.QueryAsync<Review>(
            $"""
            {SelectReviewSql}
            WHERE reviewee_user_id = @RevieweeUserId
              AND (@IncludeHidden OR (is_public = true AND moderation_status = 'published'))
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { RevieweeUserId = revieweeUserId, IncludeHidden = includeHidden, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return reviews.ToArray();
    }

    public async Task<IReadOnlyCollection<Review>> GetByReviewerAsync(Guid reviewerUserId, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var reviews = await cn.QueryAsync<Review>(
            $"""
            {SelectReviewSql}
            WHERE reviewer_user_id = @ReviewerUserId
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { ReviewerUserId = reviewerUserId, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return reviews.ToArray();
    }

    public async Task<(double AverageRating, int ReviewCount)> GetSummaryAsync(Guid revieweeUserId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var result = await cn.QuerySingleAsync<ReviewSummaryRow>(
            """
            SELECT
                COALESCE(AVG(rating), 0)::double precision AS AverageRating,
                COUNT(*)::int AS ReviewCount
            FROM reviews
            WHERE reviewee_user_id = @RevieweeUserId
              AND is_public = true
              AND moderation_status = 'published'
            """,
            new { RevieweeUserId = revieweeUserId });

        return (result.AverageRating, result.ReviewCount);
    }

    public async Task<ReviewFlag> FlagAsync(Guid reviewId, Guid flaggedByUserId, string reason, string? details, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<ReviewFlag>(
            """
            INSERT INTO review_flags (review_id, flagged_by_user_id, reason, details)
            VALUES (@ReviewId, @FlaggedByUserId, @Reason, @Details)
            ON CONFLICT (review_id, flagged_by_user_id)
            DO UPDATE SET
                reason = EXCLUDED.reason,
                details = EXCLUDED.details,
                status = 'open'
            RETURNING
                id AS Id,
                review_id AS ReviewId,
                flagged_by_user_id AS FlaggedByUserId,
                reason AS Reason,
                details AS Details,
                status AS Status,
                reviewed_by AS ReviewedBy,
                reviewed_at AS ReviewedAt,
                decision_notes AS DecisionNotes,
                created_at AS CreatedAt
            """,
            new { ReviewId = reviewId, FlaggedByUserId = flaggedByUserId, Reason = reason, Details = details });
    }

    public async Task<IReadOnlyCollection<ReviewFlag>> GetFlagsAsync(Guid reviewId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var flags = await cn.QueryAsync<ReviewFlag>(
            """
            SELECT
                id AS Id,
                review_id AS ReviewId,
                flagged_by_user_id AS FlaggedByUserId,
                reason AS Reason,
                details AS Details,
                status AS Status,
                reviewed_by AS ReviewedBy,
                reviewed_at AS ReviewedAt,
                decision_notes AS DecisionNotes,
                created_at AS CreatedAt
            FROM review_flags
            WHERE review_id = @ReviewId
            ORDER BY created_at DESC
            """,
            new { ReviewId = reviewId });

        return flags.ToArray();
    }

    public async Task<bool> ModerateAsync(Guid reviewId, string status, Guid moderatedBy, string? reason, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE reviews
            SET moderation_status = @Status,
                moderated_by = @ModeratedBy,
                moderated_at = now(),
                moderation_reason = @Reason,
                updated_at = now()
            WHERE id = @ReviewId
            """,
            new { ReviewId = reviewId, Status = status, ModeratedBy = moderatedBy, Reason = reason });

        return rows > 0;
    }

    private async Task<Review?> GetSingleAsync(string whereClause, object parameters)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<Review>(
            $"""
            {SelectReviewSql}
            WHERE {whereClause}
            """,
            parameters);
    }

    private const string SelectReviewSql =
        """
        SELECT
            id AS Id,
            help_match_id AS HelpMatchId,
            reviewer_user_id AS ReviewerUserId,
            reviewee_user_id AS RevieweeUserId,
            pet_id AS PetId,
            rating AS Rating,
            comment AS Comment,
            is_public AS IsPublic,
            moderation_status AS ModerationStatus,
            moderated_by AS ModeratedBy,
            moderated_at AS ModeratedAt,
            moderation_reason AS ModerationReason,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM reviews
        """;

    private sealed class ReviewSummaryRow
    {
        public double AverageRating { get; init; }
        public int ReviewCount { get; init; }
    }
}

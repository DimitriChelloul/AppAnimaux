using ReviewService.BLL.Models;
using ReviewService.DAL.Repositories;
using ReviewService.Domain.Entities;

namespace ReviewService.BLL.Services;

public sealed class ReviewAppService : IReviewAppService
{
    private static readonly HashSet<string> ValidFlagReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "spam",
        "harassment",
        "fake",
        "other"
    };

    private static readonly HashSet<string> ValidModerationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "published",
        "hidden",
        "flagged",
        "deleted"
    };

    private readonly IReviewRepository _reviews;

    public ReviewAppService(IReviewRepository reviews) => _reviews = reviews;

    public Task<Review> CreateAsync(Guid reviewerUserId, CreateReviewRequest request, CancellationToken ct)
    {
        ValidateReview(reviewerUserId, request);

        return _reviews.CreateAsync(
            new Review
            {
                Id = Guid.NewGuid(),
                HelpMatchId = request.HelpMatchId,
                ReviewerUserId = reviewerUserId,
                RevieweeUserId = request.RevieweeUserId,
                PetId = request.PetId,
                Rating = request.Rating,
                Comment = NormalizeOptional(request.Comment),
                IsPublic = request.IsPublic
            },
            ct);
    }

    public async Task<ReviewDetailsResponse?> GetAsync(Guid reviewId, bool includeFlags, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null)
        {
            return null;
        }

        var flags = includeFlags ? await _reviews.GetFlagsAsync(reviewId, ct) : Array.Empty<ReviewFlag>();
        return new ReviewDetailsResponse(review, flags);
    }

    public Task<IReadOnlyCollection<Review>> GetForRevieweeAsync(Guid revieweeUserId, bool includeHidden, int page, int pageSize, CancellationToken ct)
    {
        ValidatePaging(ref page, ref pageSize);
        return _reviews.GetByRevieweeAsync(revieweeUserId, includeHidden, page, pageSize, ct);
    }

    public Task<IReadOnlyCollection<Review>> GetWrittenByAsync(Guid reviewerUserId, int page, int pageSize, CancellationToken ct)
    {
        ValidatePaging(ref page, ref pageSize);
        return _reviews.GetByReviewerAsync(reviewerUserId, page, pageSize, ct);
    }

    public async Task<ReviewSummaryResponse> GetSummaryAsync(Guid revieweeUserId, CancellationToken ct)
    {
        var summary = await _reviews.GetSummaryAsync(revieweeUserId, ct);
        return new ReviewSummaryResponse(revieweeUserId, Math.Round(summary.AverageRating, 2), summary.ReviewCount);
    }

    public async Task<ReviewFlag?> FlagAsync(Guid reviewId, Guid flaggedByUserId, FlagReviewRequest request, CancellationToken ct)
    {
        if (reviewId == Guid.Empty)
        {
            throw new ArgumentException("Review id is required.");
        }

        if (flaggedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Flagged by user id is required.");
        }

        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null)
        {
            return null;
        }

        if (review.ReviewerUserId == flaggedByUserId)
        {
            throw new ArgumentException("Review author cannot flag their own review.");
        }

        var reason = NormalizeRequired(request.Reason, "Flag reason is required.").ToLowerInvariant();
        if (!ValidFlagReasons.Contains(reason))
        {
            throw new ArgumentException("Invalid review flag reason.");
        }

        return await _reviews.FlagAsync(reviewId, flaggedByUserId, reason, NormalizeOptional(request.Details), ct);
    }

    public Task<bool> ModerateAsync(Guid reviewId, Guid moderatedBy, ModerateReviewRequest request, CancellationToken ct)
    {
        if (reviewId == Guid.Empty)
        {
            throw new ArgumentException("Review id is required.");
        }

        if (moderatedBy == Guid.Empty)
        {
            throw new ArgumentException("Moderator user id is required.");
        }

        var status = NormalizeRequired(request.Status, "Moderation status is required.").ToLowerInvariant();
        if (!ValidModerationStatuses.Contains(status))
        {
            throw new ArgumentException("Invalid review moderation status.");
        }

        return _reviews.ModerateAsync(reviewId, status, moderatedBy, NormalizeOptional(request.Reason), ct);
    }

    private static void ValidateReview(Guid reviewerUserId, CreateReviewRequest request)
    {
        if (reviewerUserId == Guid.Empty)
        {
            throw new ArgumentException("Reviewer user id is required.");
        }

        if (request.HelpMatchId == Guid.Empty)
        {
            throw new ArgumentException("Help match id is required.");
        }

        if (request.RevieweeUserId == Guid.Empty)
        {
            throw new ArgumentException("Reviewee user id is required.");
        }

        if (request.RevieweeUserId == reviewerUserId)
        {
            throw new ArgumentException("Reviewer cannot review themselves.");
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }
    }

    private static void ValidatePaging(ref int page, ref int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
    }

    private static string NormalizeRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

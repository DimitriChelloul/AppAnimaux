using ReviewService.Domain.Entities;

namespace ReviewService.DAL.Repositories;

public interface IReviewRepository
{
    Task<Review> CreateAsync(Review review, CancellationToken ct);
    Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken ct);
    Task<IReadOnlyCollection<Review>> GetByRevieweeAsync(Guid revieweeUserId, bool includeHidden, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyCollection<Review>> GetByReviewerAsync(Guid reviewerUserId, int page, int pageSize, CancellationToken ct);
    Task<(double AverageRating, int ReviewCount)> GetSummaryAsync(Guid revieweeUserId, CancellationToken ct);
    Task<ReviewFlag> FlagAsync(Guid reviewId, Guid flaggedByUserId, string reason, string? details, CancellationToken ct);
    Task<IReadOnlyCollection<ReviewFlag>> GetFlagsAsync(Guid reviewId, CancellationToken ct);
    Task<bool> ModerateAsync(Guid reviewId, string status, Guid moderatedBy, string? reason, CancellationToken ct);
}

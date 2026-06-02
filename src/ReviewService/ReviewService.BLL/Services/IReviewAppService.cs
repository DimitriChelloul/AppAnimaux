using ReviewService.BLL.Models;
using ReviewService.Domain.Entities;

namespace ReviewService.BLL.Services;

public interface IReviewAppService
{
    Task<Review> CreateAsync(Guid reviewerUserId, CreateReviewRequest request, CancellationToken ct);
    Task<ReviewDetailsResponse?> GetAsync(Guid reviewId, bool includeFlags, CancellationToken ct);
    Task<IReadOnlyCollection<Review>> GetForRevieweeAsync(Guid revieweeUserId, bool includeHidden, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyCollection<Review>> GetWrittenByAsync(Guid reviewerUserId, int page, int pageSize, CancellationToken ct);
    Task<ReviewSummaryResponse> GetSummaryAsync(Guid revieweeUserId, CancellationToken ct);
    Task<ReviewFlag?> FlagAsync(Guid reviewId, Guid flaggedByUserId, FlagReviewRequest request, CancellationToken ct);
    Task<bool> ModerateAsync(Guid reviewId, Guid moderatedBy, ModerateReviewRequest request, CancellationToken ct);
}

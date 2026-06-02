using ReviewService.Domain.Entities;

namespace ReviewService.BLL.Models;

public sealed record CreateReviewRequest(
    Guid HelpMatchId,
    Guid RevieweeUserId,
    Guid? PetId,
    short Rating,
    string? Comment,
    bool IsPublic = true);

public sealed record FlagReviewRequest(string Reason, string? Details);

public sealed record ModerateReviewRequest(string Status, string? Reason);

public sealed record ReviewSummaryResponse(Guid RevieweeUserId, double AverageRating, int ReviewCount);

public sealed record ReviewDetailsResponse(Review Review, IReadOnlyCollection<ReviewFlag> Flags);

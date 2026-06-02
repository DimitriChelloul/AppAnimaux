using ReviewService.BLL.Models;
using ReviewService.BLL.Services;
using ReviewService.DAL.Repositories;
using ReviewService.Domain.Entities;

namespace ReviewService.Tests;

public sealed class ReviewAppServiceTests
{
    [Fact]
    public async Task CreateAsync_rejects_self_review()
    {
        var userId = Guid.NewGuid();
        var service = new ReviewAppService(new FakeReviewRepository());

        var request = new CreateReviewRequest(Guid.NewGuid(), userId, null, 5, "Great help");

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(userId, request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_normalizes_comment_and_creates_review()
    {
        var reviewerId = Guid.NewGuid();
        var revieweeId = Guid.NewGuid();
        var service = new ReviewAppService(new FakeReviewRepository());

        var review = await service.CreateAsync(
            reviewerId,
            new CreateReviewRequest(Guid.NewGuid(), revieweeId, null, 4, "  Reliable  "),
            CancellationToken.None);

        Assert.Equal(reviewerId, review.ReviewerUserId);
        Assert.Equal(revieweeId, review.RevieweeUserId);
        Assert.Equal("Reliable", review.Comment);
        Assert.Equal((short)4, review.Rating);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)6)]
    public async Task CreateAsync_rejects_invalid_rating(short rating)
    {
        var service = new ReviewAppService(new FakeReviewRepository());

        var request = new CreateReviewRequest(Guid.NewGuid(), Guid.NewGuid(), null, rating, null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task FlagAsync_rejects_author_flagging_own_review()
    {
        var repository = new FakeReviewRepository();
        var service = new ReviewAppService(repository);
        var reviewerId = Guid.NewGuid();
        var review = await service.CreateAsync(
            reviewerId,
            new CreateReviewRequest(Guid.NewGuid(), Guid.NewGuid(), null, 5, null),
            CancellationToken.None);

        var request = new FlagReviewRequest("fake", "Own review");

        await Assert.ThrowsAsync<ArgumentException>(() => service.FlagAsync(review.Id, reviewerId, request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSummaryAsync_rounds_average_rating()
    {
        var revieweeId = Guid.NewGuid();
        var repository = new FakeReviewRepository();
        var service = new ReviewAppService(repository);
        await service.CreateAsync(Guid.NewGuid(), new CreateReviewRequest(Guid.NewGuid(), revieweeId, null, 4, null), CancellationToken.None);
        await service.CreateAsync(Guid.NewGuid(), new CreateReviewRequest(Guid.NewGuid(), revieweeId, null, 5, null), CancellationToken.None);
        await service.CreateAsync(Guid.NewGuid(), new CreateReviewRequest(Guid.NewGuid(), Guid.NewGuid(), null, 1, null), CancellationToken.None);

        var summary = await service.GetSummaryAsync(revieweeId, CancellationToken.None);

        Assert.Equal(4.5, summary.AverageRating);
        Assert.Equal(2, summary.ReviewCount);
    }

    private sealed class FakeReviewRepository : IReviewRepository
    {
        private readonly List<Review> _reviews = [];
        private readonly List<ReviewFlag> _flags = [];

        public Task<Review> CreateAsync(Review review, CancellationToken ct)
        {
            var created = new Review
            {
                Id = review.Id,
                HelpMatchId = review.HelpMatchId,
                ReviewerUserId = review.ReviewerUserId,
                RevieweeUserId = review.RevieweeUserId,
                PetId = review.PetId,
                Rating = review.Rating,
                Comment = review.Comment,
                IsPublic = review.IsPublic,
                ModerationStatus = "published",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _reviews.Add(created);
            return Task.FromResult(created);
        }

        public Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken ct)
        {
            return Task.FromResult(_reviews.SingleOrDefault(r => r.Id == reviewId));
        }

        public Task<IReadOnlyCollection<Review>> GetByRevieweeAsync(Guid revieweeUserId, bool includeHidden, int page, int pageSize, CancellationToken ct)
        {
            var reviews = _reviews
                .Where(r => r.RevieweeUserId == revieweeUserId && (includeHidden || (r.IsPublic && r.ModerationStatus == "published")))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Review>>(reviews);
        }

        public Task<IReadOnlyCollection<Review>> GetByReviewerAsync(Guid reviewerUserId, int page, int pageSize, CancellationToken ct)
        {
            var reviews = _reviews
                .Where(r => r.ReviewerUserId == reviewerUserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Review>>(reviews);
        }

        public Task<(double AverageRating, int ReviewCount)> GetSummaryAsync(Guid revieweeUserId, CancellationToken ct)
        {
            var reviews = _reviews
                .Where(r => r.RevieweeUserId == revieweeUserId && r.IsPublic && r.ModerationStatus == "published")
                .ToArray();

            return Task.FromResult(reviews.Length == 0
                ? (0d, 0)
                : (reviews.Average(r => r.Rating), reviews.Length));
        }

        public Task<ReviewFlag> FlagAsync(Guid reviewId, Guid flaggedByUserId, string reason, string? details, CancellationToken ct)
        {
            var flag = new ReviewFlag
            {
                Id = Guid.NewGuid(),
                ReviewId = reviewId,
                FlaggedByUserId = flaggedByUserId,
                Reason = reason,
                Details = details,
                Status = "open",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _flags.Add(flag);
            return Task.FromResult(flag);
        }

        public Task<IReadOnlyCollection<ReviewFlag>> GetFlagsAsync(Guid reviewId, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyCollection<ReviewFlag>>(_flags.Where(f => f.ReviewId == reviewId).ToArray());
        }

        public Task<bool> ModerateAsync(Guid reviewId, string status, Guid moderatedBy, string? reason, CancellationToken ct)
        {
            var index = _reviews.FindIndex(r => r.Id == reviewId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var existing = _reviews[index];
            _reviews[index] = new Review
            {
                Id = existing.Id,
                HelpMatchId = existing.HelpMatchId,
                ReviewerUserId = existing.ReviewerUserId,
                RevieweeUserId = existing.RevieweeUserId,
                PetId = existing.PetId,
                Rating = existing.Rating,
                Comment = existing.Comment,
                IsPublic = existing.IsPublic,
                ModerationStatus = status,
                ModeratedBy = moderatedBy,
                ModeratedAt = DateTimeOffset.UtcNow,
                ModerationReason = reason,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return Task.FromResult(true);
        }
    }
}

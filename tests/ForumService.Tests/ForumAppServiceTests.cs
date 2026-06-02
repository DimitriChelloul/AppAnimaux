using ForumService.BLL.Models;
using ForumService.BLL.Services;
using ForumService.DAL.Repositories;
using ForumService.Domain.Entities;

namespace ForumService.Tests;

public sealed class ForumAppServiceTests
{
    [Fact]
    public async Task CreateTopicAsync_rejects_locked_category()
    {
        var repository = new FakeForumRepository();
        var service = new ForumAppService(repository);
        var category = await service.CreateCategoryAsync(new CreateForumCategoryRequest("Sante", null, null, IsLocked: true), CancellationToken.None);

        var request = new CreateForumTopicRequest(category.Id, "Question veto", "Besoin d'avis", null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTopicAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateTopicAsync_normalizes_slug_and_tags()
    {
        var repository = new FakeForumRepository();
        var service = new ForumAppService(repository);
        var category = await service.CreateCategoryAsync(new CreateForumCategoryRequest("Education canine", null, null), CancellationToken.None);

        var topic = await service.CreateTopicAsync(
            Guid.NewGuid(),
            new CreateForumTopicRequest(category.Id, "  Mon chien tire en laisse  ", "Comment travailler ca ?", null, ["Chien", "chien", " Education "]),
            CancellationToken.None);

        Assert.Equal("mon-chien-tire-en-laisse", topic.Slug);
        Assert.NotNull(topic.Tags);
        Assert.Equal(["chien", "education"], topic.Tags);
    }

    [Fact]
    public async Task CreatePostAsync_rejects_locked_topic()
    {
        var repository = new FakeForumRepository();
        var service = new ForumAppService(repository);
        var category = await service.CreateCategoryAsync(new CreateForumCategoryRequest("General", null, null), CancellationToken.None);
        var topic = await service.CreateTopicAsync(Guid.NewGuid(), new CreateForumTopicRequest(category.Id, "Sujet", "Message", null, null), CancellationToken.None);
        await service.ModerateAsync(Guid.NewGuid(), new ModerateForumContentRequest("topic", topic.Id, "locked", null), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePostAsync(Guid.NewGuid(), topic.Id, new CreateForumPostRequest("Reponse", null), CancellationToken.None));
    }

    [Fact]
    public async Task FlagAsync_rejects_invalid_reason()
    {
        var repository = new FakeForumRepository();
        var service = new ForumAppService(repository);
        var category = await service.CreateCategoryAsync(new CreateForumCategoryRequest("General", null, null), CancellationToken.None);
        var topic = await service.CreateTopicAsync(Guid.NewGuid(), new CreateForumTopicRequest(category.Id, "Sujet", "Message", null, null), CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.FlagAsync(Guid.NewGuid(), new FlagForumContentRequest("topic", topic.Id, "bad-reason", null), CancellationToken.None));
    }

    private sealed class FakeForumRepository : IForumRepository
    {
        private readonly List<ForumCategory> _categories = [];
        private readonly List<ForumTopic> _topics = [];
        private readonly List<ForumPost> _posts = [];
        private readonly List<ForumFlag> _flags = [];

        public Task<ForumCategory> CreateCategoryAsync(ForumCategory category, CancellationToken ct)
        {
            var created = new ForumCategory
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.Slug,
                IsLocked = category.IsLocked,
                SortOrder = category.SortOrder,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _categories.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyCollection<ForumCategory>> GetCategoriesAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyCollection<ForumCategory>>(_categories.ToArray());
        }

        public Task<ForumCategory?> GetCategoryAsync(Guid categoryId, CancellationToken ct)
        {
            return Task.FromResult(_categories.SingleOrDefault(c => c.Id == categoryId));
        }

        public Task<ForumTopic> CreateTopicAsync(ForumTopic topic, CancellationToken ct)
        {
            var created = new ForumTopic
            {
                Id = topic.Id,
                CategoryId = topic.CategoryId,
                AuthorUserId = topic.AuthorUserId,
                Title = topic.Title,
                Slug = topic.Slug,
                Content = topic.Content,
                AttachmentsJson = topic.AttachmentsJson,
                Tags = topic.Tags,
                Status = "open",
                LastPostAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _topics.Add(created);
            return Task.FromResult(created);
        }

        public Task<ForumTopic?> GetTopicAsync(Guid topicId, CancellationToken ct)
        {
            return Task.FromResult(_topics.SingleOrDefault(t => t.Id == topicId));
        }

        public Task<IReadOnlyCollection<ForumTopic>> GetTopicsAsync(Guid? categoryId, string? status, int page, int pageSize, CancellationToken ct)
        {
            var topics = _topics
                .Where(t => categoryId is null || t.CategoryId == categoryId)
                .Where(t => status is null || t.Status == status)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ForumTopic>>(topics);
        }

        public Task IncrementTopicViewsAsync(Guid topicId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<ForumPost> CreatePostAsync(ForumPost post, CancellationToken ct)
        {
            var created = new ForumPost
            {
                Id = post.Id,
                TopicId = post.TopicId,
                AuthorUserId = post.AuthorUserId,
                Content = post.Content,
                AttachmentsJson = post.AttachmentsJson,
                Status = "published",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _posts.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyCollection<ForumPost>> GetPostsAsync(Guid topicId, bool includeHidden, int page, int pageSize, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyCollection<ForumPost>>(_posts.Where(p => p.TopicId == topicId).ToArray());
        }

        public Task<ForumPost?> GetPostAsync(Guid postId, CancellationToken ct)
        {
            return Task.FromResult(_posts.SingleOrDefault(p => p.Id == postId));
        }

        public Task<bool> UpdatePostAsync(Guid postId, Guid authorUserId, string content, string? attachmentsJson, CancellationToken ct)
        {
            return Task.FromResult(_posts.Any(p => p.Id == postId && p.AuthorUserId == authorUserId));
        }

        public Task<bool> DeletePostAsync(Guid postId, Guid authorUserId, CancellationToken ct)
        {
            return Task.FromResult(_posts.Any(p => p.Id == postId && p.AuthorUserId == authorUserId));
        }

        public Task<ForumFlag> FlagAsync(Guid flaggedByUserId, string targetType, Guid targetId, string reason, string? details, CancellationToken ct)
        {
            var flag = new ForumFlag
            {
                Id = Guid.NewGuid(),
                FlaggedByUserId = flaggedByUserId,
                TargetType = targetType,
                TargetId = targetId,
                Reason = reason,
                Details = details,
                Status = "open",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _flags.Add(flag);
            return Task.FromResult(flag);
        }

        public Task<bool> ModerateAsync(string targetType, Guid targetId, string status, Guid reviewedBy, string? reason, CancellationToken ct)
        {
            if (targetType == "topic")
            {
                var index = _topics.FindIndex(t => t.Id == targetId);
                if (index < 0)
                {
                    return Task.FromResult(false);
                }

                var existing = _topics[index];
                _topics[index] = new ForumTopic
                {
                    Id = existing.Id,
                    CategoryId = existing.CategoryId,
                    AuthorUserId = existing.AuthorUserId,
                    Title = existing.Title,
                    Slug = existing.Slug,
                    Content = existing.Content,
                    AttachmentsJson = existing.AttachmentsJson,
                    Tags = existing.Tags,
                    Status = status,
                    IsPinned = existing.IsPinned,
                    ViewsCount = existing.ViewsCount,
                    RepliesCount = existing.RepliesCount,
                    LastPostAt = existing.LastPostAt,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                return Task.FromResult(true);
            }

            return Task.FromResult(_posts.Any(p => p.Id == targetId));
        }
    }
}

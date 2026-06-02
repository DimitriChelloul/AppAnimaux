using ForumService.Domain.Entities;

namespace ForumService.DAL.Repositories;

public interface IForumRepository
{
    Task<ForumCategory> CreateCategoryAsync(ForumCategory category, CancellationToken ct);
    Task<IReadOnlyCollection<ForumCategory>> GetCategoriesAsync(CancellationToken ct);
    Task<ForumCategory?> GetCategoryAsync(Guid categoryId, CancellationToken ct);
    Task<ForumTopic> CreateTopicAsync(ForumTopic topic, CancellationToken ct);
    Task<ForumTopic?> GetTopicAsync(Guid topicId, CancellationToken ct);
    Task<IReadOnlyCollection<ForumTopic>> GetTopicsAsync(Guid? categoryId, string? status, int page, int pageSize, CancellationToken ct);
    Task IncrementTopicViewsAsync(Guid topicId, CancellationToken ct);
    Task<ForumPost> CreatePostAsync(ForumPost post, CancellationToken ct);
    Task<IReadOnlyCollection<ForumPost>> GetPostsAsync(Guid topicId, bool includeHidden, int page, int pageSize, CancellationToken ct);
    Task<ForumPost?> GetPostAsync(Guid postId, CancellationToken ct);
    Task<bool> UpdatePostAsync(Guid postId, Guid authorUserId, string content, string? attachmentsJson, CancellationToken ct);
    Task<bool> DeletePostAsync(Guid postId, Guid authorUserId, CancellationToken ct);
    Task<ForumFlag> FlagAsync(Guid flaggedByUserId, string targetType, Guid targetId, string reason, string? details, CancellationToken ct);
    Task<bool> ModerateAsync(string targetType, Guid targetId, string status, Guid reviewedBy, string? reason, CancellationToken ct);
}

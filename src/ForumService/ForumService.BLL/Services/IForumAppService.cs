using ForumService.BLL.Models;

namespace ForumService.BLL.Services;

public interface IForumAppService
{
    Task<ForumCategoryResponse> CreateCategoryAsync(CreateForumCategoryRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<ForumCategoryResponse>> GetCategoriesAsync(CancellationToken ct);
    Task<ForumTopicResponse> CreateTopicAsync(Guid authorUserId, CreateForumTopicRequest request, CancellationToken ct);
    Task<ForumTopicResponse?> GetTopicAsync(Guid topicId, bool incrementViews, CancellationToken ct);
    Task<IReadOnlyCollection<ForumTopicResponse>> GetTopicsAsync(Guid? categoryId, string? status, int page, int pageSize, CancellationToken ct);
    Task<ForumPostResponse?> CreatePostAsync(Guid authorUserId, Guid topicId, CreateForumPostRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<ForumPostResponse>> GetPostsAsync(Guid topicId, bool includeHidden, int page, int pageSize, CancellationToken ct);
    Task<bool> UpdatePostAsync(Guid authorUserId, Guid postId, UpdateForumPostRequest request, CancellationToken ct);
    Task<bool> DeletePostAsync(Guid authorUserId, Guid postId, CancellationToken ct);
    Task<ForumFlagResponse?> FlagAsync(Guid flaggedByUserId, FlagForumContentRequest request, CancellationToken ct);
    Task<bool> ModerateAsync(Guid reviewedBy, ModerateForumContentRequest request, CancellationToken ct);
}

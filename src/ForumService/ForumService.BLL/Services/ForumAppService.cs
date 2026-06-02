using ForumService.BLL.Models;
using ForumService.DAL.Repositories;
using ForumService.Domain.Entities;

namespace ForumService.BLL.Services;

public sealed class ForumAppService : IForumAppService
{
    private static readonly HashSet<string> ValidTargetTypes = new(StringComparer.OrdinalIgnoreCase) { "topic", "post" };
    private static readonly HashSet<string> ValidFlagReasons = new(StringComparer.OrdinalIgnoreCase) { "spam", "harassment", "hate", "illegal", "other" };
    private static readonly HashSet<string> ValidTopicStatuses = new(StringComparer.OrdinalIgnoreCase) { "open", "locked", "hidden", "deleted" };
    private static readonly HashSet<string> ValidPostStatuses = new(StringComparer.OrdinalIgnoreCase) { "published", "hidden", "deleted" };

    private readonly IForumRepository _forum;

    public ForumAppService(IForumRepository forum) => _forum = forum;

    public async Task<ForumCategoryResponse> CreateCategoryAsync(CreateForumCategoryRequest request, CancellationToken ct)
    {
        var name = NormalizeRequired(request.Name, "Category name is required.");
        var slug = Slugify(string.IsNullOrWhiteSpace(request.Slug) ? name : request.Slug);

        var category = await _forum.CreateCategoryAsync(
            new ForumCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = NormalizeOptional(request.Description),
                Slug = slug,
                IsLocked = request.IsLocked,
                SortOrder = request.SortOrder
            },
            ct);

        return ToCategoryResponse(category);
    }

    public async Task<IReadOnlyCollection<ForumCategoryResponse>> GetCategoriesAsync(CancellationToken ct)
    {
        var categories = await _forum.GetCategoriesAsync(ct);
        return categories.Select(ToCategoryResponse).ToArray();
    }

    public async Task<ForumTopicResponse> CreateTopicAsync(Guid authorUserId, CreateForumTopicRequest request, CancellationToken ct)
    {
        if (authorUserId == Guid.Empty)
        {
            throw new ArgumentException("Author user id is required.");
        }

        if (request.CategoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.");
        }

        var category = await _forum.GetCategoryAsync(request.CategoryId, ct);
        if (category is null)
        {
            throw new InvalidOperationException("Forum category does not exist.");
        }

        if (category.IsLocked)
        {
            throw new InvalidOperationException("Forum category is locked.");
        }

        var title = NormalizeRequired(request.Title, "Topic title is required.");
        var content = NormalizeOptional(request.Content);
        if (content is null)
        {
            throw new ArgumentException("Topic content is required.");
        }

        var topic = await _forum.CreateTopicAsync(
            new ForumTopic
            {
                Id = Guid.NewGuid(),
                CategoryId = request.CategoryId,
                AuthorUserId = authorUserId,
                Title = title,
                Slug = Slugify(title),
                Content = content,
                AttachmentsJson = NormalizeJson(request.AttachmentsJson),
                Tags = NormalizeTags(request.Tags)
            },
            ct);

        return ToTopicResponse(topic);
    }

    public async Task<ForumTopicResponse?> GetTopicAsync(Guid topicId, bool incrementViews, CancellationToken ct)
    {
        var topic = await _forum.GetTopicAsync(topicId, ct);
        if (topic is null)
        {
            return null;
        }

        if (incrementViews)
        {
            await _forum.IncrementTopicViewsAsync(topicId, ct);
            topic = await _forum.GetTopicAsync(topicId, ct) ?? topic;
        }

        return ToTopicResponse(topic);
    }

    public async Task<IReadOnlyCollection<ForumTopicResponse>> GetTopicsAsync(Guid? categoryId, string? status, int page, int pageSize, CancellationToken ct)
    {
        ValidatePaging(ref page, ref pageSize);
        var normalizedStatus = NormalizeOptional(status)?.ToLowerInvariant();
        if (normalizedStatus is not null && !ValidTopicStatuses.Contains(normalizedStatus))
        {
            throw new ArgumentException("Invalid topic status.");
        }

        var topics = await _forum.GetTopicsAsync(categoryId, normalizedStatus, page, pageSize, ct);
        return topics.Select(ToTopicResponse).ToArray();
    }

    public async Task<ForumPostResponse?> CreatePostAsync(Guid authorUserId, Guid topicId, CreateForumPostRequest request, CancellationToken ct)
    {
        if (authorUserId == Guid.Empty)
        {
            throw new ArgumentException("Author user id is required.");
        }

        var topic = await _forum.GetTopicAsync(topicId, ct);
        if (topic is null)
        {
            return null;
        }

        if (topic.Status != "open")
        {
            throw new InvalidOperationException("Topic is not open.");
        }

        var post = await _forum.CreatePostAsync(
            new ForumPost
            {
                Id = Guid.NewGuid(),
                TopicId = topicId,
                AuthorUserId = authorUserId,
                Content = NormalizeRequired(request.Content, "Post content is required."),
                AttachmentsJson = NormalizeJson(request.AttachmentsJson)
            },
            ct);

        return ToPostResponse(post);
    }

    public async Task<IReadOnlyCollection<ForumPostResponse>> GetPostsAsync(Guid topicId, bool includeHidden, int page, int pageSize, CancellationToken ct)
    {
        ValidatePaging(ref page, ref pageSize);
        var posts = await _forum.GetPostsAsync(topicId, includeHidden, page, pageSize, ct);
        return posts.Select(ToPostResponse).ToArray();
    }

    public Task<bool> UpdatePostAsync(Guid authorUserId, Guid postId, UpdateForumPostRequest request, CancellationToken ct)
    {
        if (authorUserId == Guid.Empty)
        {
            throw new ArgumentException("Author user id is required.");
        }

        return _forum.UpdatePostAsync(postId, authorUserId, NormalizeRequired(request.Content, "Post content is required."), NormalizeJson(request.AttachmentsJson), ct);
    }

    public Task<bool> DeletePostAsync(Guid authorUserId, Guid postId, CancellationToken ct)
    {
        if (authorUserId == Guid.Empty)
        {
            throw new ArgumentException("Author user id is required.");
        }

        return _forum.DeletePostAsync(postId, authorUserId, ct);
    }

    public async Task<ForumFlagResponse?> FlagAsync(Guid flaggedByUserId, FlagForumContentRequest request, CancellationToken ct)
    {
        if (flaggedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Flagged by user id is required.");
        }

        var targetType = NormalizeTargetType(request.TargetType);
        if (!await TargetExistsAsync(targetType, request.TargetId, ct))
        {
            return null;
        }

        var reason = NormalizeRequired(request.Reason, "Flag reason is required.").ToLowerInvariant();
        if (!ValidFlagReasons.Contains(reason))
        {
            throw new ArgumentException("Invalid forum flag reason.");
        }

        var flag = await _forum.FlagAsync(flaggedByUserId, targetType, request.TargetId, reason, NormalizeOptional(request.Details), ct);
        return ToFlagResponse(flag);
    }

    public async Task<bool> ModerateAsync(Guid reviewedBy, ModerateForumContentRequest request, CancellationToken ct)
    {
        if (reviewedBy == Guid.Empty)
        {
            throw new ArgumentException("Reviewer user id is required.");
        }

        var targetType = NormalizeTargetType(request.TargetType);
        var status = NormalizeRequired(request.Status, "Moderation status is required.").ToLowerInvariant();
        var validStatuses = targetType == "topic" ? ValidTopicStatuses : ValidPostStatuses;
        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException("Invalid forum moderation status.");
        }

        return await _forum.ModerateAsync(targetType, request.TargetId, status, reviewedBy, NormalizeOptional(request.Reason), ct);
    }

    private async Task<bool> TargetExistsAsync(string targetType, Guid targetId, CancellationToken ct)
    {
        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("Target id is required.");
        }

        return targetType == "topic"
            ? await _forum.GetTopicAsync(targetId, ct) is not null
            : await _forum.GetPostAsync(targetId, ct) is not null;
    }

    private static string NormalizeTargetType(string value)
    {
        var targetType = NormalizeRequired(value, "Target type is required.").ToLowerInvariant();
        if (!ValidTargetTypes.Contains(targetType))
        {
            throw new ArgumentException("Invalid forum target type.");
        }

        return targetType;
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

    private static string? NormalizeJson(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string[]? NormalizeTags(string[]? tags)
    {
        var normalized = tags?
            .Select(NormalizeOptional)
            .Where(tag => tag is not null)
            .Select(tag => tag!.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        return normalized is { Length: > 0 } ? normalized : null;
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static ForumCategoryResponse ToCategoryResponse(ForumCategory category) =>
        new(category.Id, category.Name, category.Description, category.Slug, category.IsLocked, category.SortOrder);

    private static ForumTopicResponse ToTopicResponse(ForumTopic topic) =>
        new(topic.Id, topic.CategoryId, topic.AuthorUserId, topic.Title, topic.Slug, topic.Content, topic.AttachmentsJson, topic.Tags, topic.Status, topic.IsPinned, topic.ViewsCount, topic.RepliesCount, topic.LastPostAt, topic.CreatedAt);

    private static ForumPostResponse ToPostResponse(ForumPost post) =>
        new(post.Id, post.TopicId, post.AuthorUserId, post.Content, post.AttachmentsJson, post.Status, post.EditedAt, post.CreatedAt);

    private static ForumFlagResponse ToFlagResponse(ForumFlag flag) =>
        new(flag.Id, flag.FlaggedByUserId, flag.TargetType, flag.TargetId, flag.Reason, flag.Details, flag.Status);
}

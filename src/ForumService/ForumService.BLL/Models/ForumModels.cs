namespace ForumService.BLL.Models;

public sealed record CreateForumCategoryRequest(string Name, string? Description, string? Slug, bool IsLocked = false, int SortOrder = 0);

public sealed record CreateForumTopicRequest(Guid CategoryId, string Title, string? Content, string? AttachmentsJson, string[]? Tags);

public sealed record CreateForumPostRequest(string Content, string? AttachmentsJson);

public sealed record UpdateForumPostRequest(string Content, string? AttachmentsJson);

public sealed record FlagForumContentRequest(string TargetType, Guid TargetId, string Reason, string? Details);

public sealed record ModerateForumContentRequest(string TargetType, Guid TargetId, string Status, string? Reason);

public sealed record ForumCategoryResponse(Guid Id, string Name, string? Description, string Slug, bool IsLocked, int SortOrder);

public sealed record ForumTopicResponse(
    Guid Id,
    Guid CategoryId,
    Guid AuthorUserId,
    string Title,
    string? Slug,
    string? Content,
    string? AttachmentsJson,
    string[]? Tags,
    string Status,
    bool IsPinned,
    long ViewsCount,
    long RepliesCount,
    DateTimeOffset? LastPostAt,
    DateTimeOffset CreatedAt);

public sealed record ForumPostResponse(
    Guid Id,
    Guid TopicId,
    Guid AuthorUserId,
    string Content,
    string? AttachmentsJson,
    string Status,
    DateTimeOffset? EditedAt,
    DateTimeOffset CreatedAt);

public sealed record ForumFlagResponse(Guid Id, Guid FlaggedByUserId, string TargetType, Guid TargetId, string Reason, string? Details, string Status);

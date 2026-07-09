namespace Shared.TimeId;

public static class TimeIds
{
    public static Guid NewId() => Guid.CreateVersion7();
}
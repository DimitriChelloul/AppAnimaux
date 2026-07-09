using Shared.TimeId;

namespace Shared.Testing;

public sealed class TestClock : IClock
{
    public TestClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    public void Set(DateTimeOffset utcNow) => UtcNow = utcNow;
}
using Shared.Contracts.Events.Abstractions;

namespace Shared.Contracts.Events.Users;

public sealed record UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; init; }
}

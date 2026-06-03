using Shared.Contracts.Events.Users;
using Shared.Messaging.Abstractions;
using UserProfileService.DAL.Repositories;
using UserProfileService.Domain.Entities;

namespace UserProfileService.BLL.Handlers;

public sealed class UserRegisteredHandler : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly IUserProfileRepository _profiles;

    public UserRegisteredHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task HandleAsync(UserRegisteredEvent evt, CancellationToken ct)
    {
        var existing = await _profiles.GetByUserIdAsync(evt.UserId, ct);
        if (existing is not null)
        {
            return;
        }

        await _profiles.UpsertAsync(new UserProfile { UserId = evt.UserId }, ct);
    }
}

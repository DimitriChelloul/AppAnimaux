using System.Text.Json;
using AlertService.DAL.Repositories;
using Shared.Contracts.Events.HelpRequests;
using Shared.Contracts.Messaging;

namespace AlertService.BLL.Handlers;

public sealed class HelpRequestNotificationHandler
{
    private readonly INotificationRepository _notifications;

    public HelpRequestNotificationHandler(INotificationRepository notifications) => _notifications = notifications;

    public Task HandleHelpOfferCreatedAsync(HelpOfferCreatedEvent evt, CancellationToken ct)
    {
        var data = SerializeData(new
        {
            evt.HelpRequestId,
            evt.HelpOfferId,
            evt.HelperUserId,
            route = $"/help-requests/{evt.HelpRequestId}"
        });

        return _notifications.CreateAsync(
            evt.RequesterUserId,
            "Nouvelle proposition",
            $"Une personne a proposé son aide pour \"{evt.Title}\".",
            EventTypes.HelpRequests.HelpOfferCreated,
            data,
            "high",
            ct);
    }

    public Task HandleHelpOfferAcceptedAsync(HelpOfferAcceptedEvent evt, CancellationToken ct)
    {
        var data = SerializeData(new
        {
            evt.HelpRequestId,
            evt.HelpOfferId,
            evt.HelpMatchId,
            evt.RequesterUserId,
            route = $"/help-requests/{evt.HelpRequestId}"
        });

        return _notifications.CreateAsync(
            evt.HelperUserId,
            "Proposition acceptée",
            $"Votre proposition pour \"{evt.Title}\" a été acceptée.",
            EventTypes.HelpRequests.HelpOfferAccepted,
            data,
            "high",
            ct);
    }

    private static string SerializeData(object value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}

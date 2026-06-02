using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Messaging.Routing;

using Shared.Contracts.Messaging;

public sealed class DefaultEventRoutingMapper : IEventRoutingMapper
{
    private static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // Payments
        [EventTypes.Payments.PaymentIntentCreated] = RoutingKeys.Payments.IntentCreated,
        [EventTypes.Payments.PaymentSucceeded] = RoutingKeys.Payments.Succeeded,
        [EventTypes.Payments.PaymentFailed] = RoutingKeys.Payments.Failed,
        [EventTypes.Payments.RefundSucceeded] = RoutingKeys.Payments.RefundSucceeded,
        [EventTypes.Payments.RefundFailed] = RoutingKeys.Payments.RefundFailed,

        // Subscriptions
        [EventTypes.Subscriptions.SubscriptionCreated] = RoutingKeys.Subscriptions.Created,
        [EventTypes.Subscriptions.SubscriptionActivated] = RoutingKeys.Subscriptions.Activated,
        [EventTypes.Subscriptions.SubscriptionRenewed] = RoutingKeys.Subscriptions.Renewed,
        [EventTypes.Subscriptions.SubscriptionCanceled] = RoutingKeys.Subscriptions.Canceled,
        [EventTypes.Subscriptions.SubscriptionExpired] = RoutingKeys.Subscriptions.Expired,
        [EventTypes.Subscriptions.SubscriptionPastDue] = RoutingKeys.Subscriptions.PastDue,
        [EventTypes.Subscriptions.SubscriptionPlanChanged] = RoutingKeys.Subscriptions.PlanChanged,

        // Credits
        [EventTypes.Credits.WalletCreated] = RoutingKeys.Credits.WalletCreated,
        [EventTypes.Credits.CreditsGranted] = RoutingKeys.Credits.Granted,
        [EventTypes.Credits.CreditsReserved] = RoutingKeys.Credits.Reserved,
        [EventTypes.Credits.CreditsSpent] = RoutingKeys.Credits.Spent,
        [EventTypes.Credits.CreditsReservationCanceled] = RoutingKeys.Credits.ReservationCanceled,
        [EventTypes.Credits.CreditsRefunded] = RoutingKeys.Credits.Refunded,
        [EventTypes.Credits.CreditsAdjusted] = RoutingKeys.Credits.Adjusted,

        // Advertising
        [EventTypes.Advertising.CampaignCreated] = RoutingKeys.Advertising.CampaignCreated,
        [EventTypes.Advertising.CampaignActivated] = RoutingKeys.Advertising.CampaignActivated,
        [EventTypes.Advertising.CampaignPaused] = RoutingKeys.Advertising.CampaignPaused,
        [EventTypes.Advertising.ImpressionTracked] = RoutingKeys.Advertising.ImpressionTracked,
        [EventTypes.Advertising.ClickTracked] = RoutingKeys.Advertising.ClickTracked,
        [EventTypes.Advertising.BudgetReached] = RoutingKeys.Advertising.BudgetReached,

        // Help requests
        [EventTypes.HelpRequests.HelpRequestCreated] = RoutingKeys.HelpRequests.Created,
        [EventTypes.HelpRequests.HelpRequestPublished] = RoutingKeys.HelpRequests.Published,
        [EventTypes.HelpRequests.HelpOfferCreated] = RoutingKeys.HelpRequests.OfferCreated,
        [EventTypes.HelpRequests.HelpOfferAccepted] = RoutingKeys.HelpRequests.OfferAccepted,
        [EventTypes.HelpRequests.HelpMatchCompleted] = RoutingKeys.HelpRequests.Completed,

        // Messaging
        [EventTypes.Messaging.MessageSent] = RoutingKeys.Messaging.MessageSent,
    };

    public string GetRoutingKey(string eventType)
        => Map.TryGetValue(eventType, out var rk)
            ? rk
            : throw new InvalidOperationException($"No routing key mapping for event type '{eventType}'.");
}


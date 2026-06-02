using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Messaging;

/// <summary>
/// Routing keys RabbitMQ (topic exchange).
/// Convention: <domain>.<event>.<version>
/// Exemple: payment.succeeded.v1
/// </summary>
public static class RoutingKeys
{
    // Versioning
    public const string V1 = "v1";

    public static class Payments
    {
        public const string IntentCreated = "payment.intent_created.v1";
        public const string Succeeded = "payment.succeeded.v1";
        public const string Failed = "payment.failed.v1";
        public const string RefundSucceeded = "payment.refund_succeeded.v1";
        public const string RefundFailed = "payment.refund_failed.v1";
    }

    public static class Subscriptions
    {
        public const string Created = "subscription.created.v1";
        public const string Activated = "subscription.activated.v1";
        public const string Renewed = "subscription.renewed.v1";
        public const string Canceled = "subscription.canceled.v1";
        public const string Expired = "subscription.expired.v1";
        public const string PastDue = "subscription.past_due.v1";
        public const string PlanChanged = "subscription.plan_changed.v1";
    }

    public static class Credits
    {
        public const string WalletCreated = "credits.wallet_created.v1";
        public const string Granted = "credits.granted.v1";
        public const string Reserved = "credits.reserved.v1";
        public const string Spent = "credits.spent.v1";
        public const string ReservationCanceled = "credits.reservation_canceled.v1";
        public const string Refunded = "credits.refunded.v1";
        public const string Adjusted = "credits.adjusted.v1";
    }

    public static class Advertising
    {
        public const string CampaignCreated = "ads.campaign_created.v1";
        public const string CampaignActivated = "ads.campaign_activated.v1";
        public const string CampaignPaused = "ads.campaign_paused.v1";
        public const string ImpressionTracked = "ads.impression_tracked.v1";
        public const string ClickTracked = "ads.click_tracked.v1";
        public const string BudgetReached = "ads.budget_reached.v1";
    }

    public static class HelpRequests
    {
        public const string Created = "helprequest.created.v1";
        public const string Published = "helprequest.published.v1";
        public const string OfferCreated = "helprequest.offer_created.v1";
        public const string OfferAccepted = "helprequest.offer_accepted.v1";
        public const string Completed = "helprequest.completed.v1";
    }

    public static class Messaging
    {
        public const string MessageSent = "messaging.message_sent.v1";
    }
}


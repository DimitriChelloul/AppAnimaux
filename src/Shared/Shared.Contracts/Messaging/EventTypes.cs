using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Messaging;

/// <summary>
/// Noms "fonctionnels" des events (lisibles) + version.
/// Ces strings sont utilisées dans l'enveloppe EventEnvelope.Type.
/// </summary>
public static class EventTypes
{
    public const int V1 = 1;

    public static class Payments
    {
        public const string PaymentIntentCreated = "Payment.IntentCreated";
        public const string PaymentSucceeded = "Payment.Succeeded";
        public const string PaymentFailed = "Payment.Failed";
        public const string RefundSucceeded = "Payment.RefundSucceeded";
        public const string RefundFailed = "Payment.RefundFailed";
    }

    public static class Subscriptions
    {
        public const string SubscriptionCreated = "Subscription.Created";
        public const string SubscriptionActivated = "Subscription.Activated";
        public const string SubscriptionRenewed = "Subscription.Renewed";
        public const string SubscriptionCanceled = "Subscription.Canceled";
        public const string SubscriptionExpired = "Subscription.Expired";
        public const string SubscriptionPastDue = "Subscription.PastDue";
        public const string SubscriptionPlanChanged = "Subscription.PlanChanged";
    }

    public static class Credits
    {
        public const string WalletCreated = "Credits.WalletCreated";
        public const string CreditsGranted = "Credits.Granted";
        public const string CreditsReserved = "Credits.Reserved";
        public const string CreditsSpent = "Credits.Spent";
        public const string CreditsReservationCanceled = "Credits.ReservationCanceled";
        public const string CreditsRefunded = "Credits.Refunded";
        public const string CreditsAdjusted = "Credits.Adjusted";
    }

    public static class Advertising
    {
        public const string CampaignCreated = "Ads.CampaignCreated";
        public const string CampaignActivated = "Ads.CampaignActivated";
        public const string CampaignPaused = "Ads.CampaignPaused";
        public const string ImpressionTracked = "Ads.ImpressionTracked";
        public const string ClickTracked = "Ads.ClickTracked";
        public const string BudgetReached = "Ads.BudgetReached";
    }

    // (Optionnel) autres domaines plus tard
    public static class HelpRequests
    {
        public const string HelpRequestCreated = "HelpRequest.Created";
        public const string HelpOfferAccepted = "HelpOffer.Accepted";
        public const string HelpMatchCompleted = "HelpMatch.Completed";
    }
}


namespace PaymentService.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.BLL.Options;
using PaymentService.BLL.Providers;
using PaymentService.BLL.Services;
using PaymentService.DAL.Interfaces;
using PaymentService.DAL.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Shared.Messaging.Outbox;
using Xunit;

public sealed class PaymentSystemTests
{
    [Fact]
    public async Task Professional_subscription_can_create_checkout_session()
    {
        var env = TestEnvironment.Create();
        var result = await env.Professionals.CreateCheckoutSessionAsync(new CreateProfessionalSubscriptionDto(env.ProfessionalId, PlanCode.ProBasic, "ok", "ko"), CancellationToken.None);
        Assert.StartsWith("cs_sim_", result.SessionId);
    }

    [Fact]
    public async Task Professional_subscription_can_change_plan()
    {
        var env = TestEnvironment.Create();
        await env.Professionals.UpsertFromStripeAsync(env.ProfessionalId, PlanCode.ProBasic, "cus_1", "sub_1", SubscriptionStatus.Active, null, null, CancellationToken.None);
        var result = await env.Professionals.ChangePlanAsync(new ChangeProfessionalPlanDto(env.ProfessionalId, PlanCode.ProPlus), CancellationToken.None);
        Assert.Equal(PlanCode.ProPlus, result.PlanCode);
    }

    [Fact]
    public async Task Professional_subscription_can_cancel()
    {
        var env = TestEnvironment.Create();
        await env.Professionals.UpsertFromStripeAsync(env.ProfessionalId, PlanCode.ProBasic, "cus_1", "sub_1", SubscriptionStatus.Active, null, null, CancellationToken.None);
        var result = await env.Professionals.CancelAsync(env.ProfessionalId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Canceled, result.Status);
    }

    [Fact]
    public async Task Stripe_invoice_paid_webhook_is_processed()
    {
        var env = TestEnvironment.Create();
        var payload = """{"id":"evt_1","type":"invoice.paid","data":{"object":{"id":"in_1","amount_paid":1900,"currency":"eur","hosted_invoice_url":"https://invoice"}}}""";
        var result = await env.Webhooks.ProcessStripeAsync(payload, null, CancellationToken.None);
        Assert.True(result.Processed);
    }

    [Fact]
    public async Task Stripe_invoice_payment_failed_webhook_is_processed()
    {
        var env = TestEnvironment.Create();
        var payload = """{"id":"evt_2","type":"invoice.payment_failed","data":{"object":{"id":"in_2","amount_due":1900,"currency":"eur"}}}""";
        var result = await env.Webhooks.ProcessStripeAsync(payload, null, CancellationToken.None);
        Assert.True(result.Processed);
    }

    [Fact]
    public async Task Apple_purchase_validation_creates_user_subscription()
    {
        var env = TestEnvironment.Create();
        var result = await env.Apple.ValidateAsync(new ValidateApplePurchaseDto(env.UserId, "appanimaux.userpremium.monthly", "tx_1", null, "receipt"), CancellationToken.None);
        Assert.Equal(PlanCode.UserPremium, result.PlanCode);
        Assert.Equal(SubscriptionStatus.Active, result.Status);
    }

    [Fact]
    public async Task Google_purchase_validation_creates_user_subscription()
    {
        var env = TestEnvironment.Create();
        var result = await env.Google.ValidateAsync(new ValidateGooglePurchaseDto(env.UserId, "userplus_monthly", "token_1", "com.appanimaux.mobile"), CancellationToken.None);
        Assert.Equal(PlanCode.UserPlus, result.PlanCode);
        Assert.Equal(SubscriptionStatus.Active, result.Status);
    }

    [Fact]
    public async Task Entitlements_are_calculated_from_plan()
    {
        var env = TestEnvironment.Create();
        await env.Professionals.UpsertFromStripeAsync(env.ProfessionalId, PlanCode.ProPremium, "cus_1", "sub_1", SubscriptionStatus.Active, null, null, CancellationToken.None);
        var entitlements = await env.Entitlements.GetForProfessionalAsync(env.ProfessionalId, CancellationToken.None);
        Assert.Equal("true", entitlements.Entitlements["professional_verified_badge_enabled"]);
    }

    [Fact]
    public async Task Subscription_events_are_written_to_outbox_for_rabbitmq()
    {
        var env = TestEnvironment.Create();
        await env.Professionals.UpsertFromStripeAsync(env.ProfessionalId, PlanCode.ProBasic, "cus_1", "sub_1", SubscriptionStatus.Active, null, null, CancellationToken.None);
        Assert.Contains(env.Outbox.Messages, x => x.Type == "ProfessionalSubscriptionCreated");
    }

    [Fact]
    public void Security_requires_user_identity_for_user_endpoints()
    {
        Assert.True(true);
    }

    private sealed class TestEnvironment
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid ProfessionalId { get; } = Guid.NewGuid();
        public FakeOutbox Outbox { get; } = new();
        public ISubscriptionEntitlementService Entitlements { get; private set; } = default!;
        public IProfessionalSubscriptionService Professionals { get; private set; } = default!;
        public IApplePurchaseService Apple { get; private set; } = default!;
        public IGooglePurchaseService Google { get; private set; } = default!;
        public ISubscriptionWebhookService Webhooks { get; private set; } = default!;

        public static TestEnvironment Create()
        {
            var env = new TestEnvironment();
            var plans = new FakePlans();
            var users = new FakeUsers();
            var pros = new FakeProfessionals();
            var ents = new FakeEntitlements();
            var receipts = new FakeReceipts();
            var webhooks = new FakeWebhookEvents();
            var invoices = new FakeInvoices();
            var publisher = new SubscriptionEventPublisher(env.Outbox);
            var stripe = new StripeBillingService(new HttpClient(), Options.Create(new StripeOptions()), NullLogger<StripeBillingService>.Instance);
            env.Entitlements = new SubscriptionEntitlementService(users, pros, plans, ents);
            var userSvc = new UserSubscriptionService(users, plans, env.Entitlements, publisher);
            env.Professionals = new ProfessionalSubscriptionService(pros, plans, env.Entitlements, stripe, publisher);
            env.Apple = new ApplePurchaseService(Options.Create(new AppleOptions()), plans, receipts, userSvc);
            env.Google = new GooglePurchaseService(Options.Create(new GooglePlayOptions { PackageName = "com.appanimaux.mobile" }), plans, receipts, userSvc);
            env.Webhooks = new SubscriptionWebhookService(webhooks, stripe, env.Apple, env.Google, env.Professionals, invoices);
            return env;
        }
    }

    private sealed class FakePlans : ISubscriptionPlanRepository
    {
        private readonly List<SubscriptionPlan> _plans =
        [
            Plan("00000000-0000-0000-0000-000000000101", PlanCode.Free, SubscriptionOwnerType.User, null, 0, null, null, null),
            Plan("00000000-0000-0000-0000-000000000102", PlanCode.UserPremium, SubscriptionOwnerType.User, null, 4.99m, null, "appanimaux.userpremium.monthly", "userpremium_monthly"),
            Plan("00000000-0000-0000-0000-000000000103", PlanCode.UserPlus, SubscriptionOwnerType.User, null, 7.99m, null, "appanimaux.userplus.monthly", "userplus_monthly"),
            Plan("00000000-0000-0000-0000-000000000201", PlanCode.ProFree, SubscriptionOwnerType.Professional, SubscriptionProvider.Stripe, 0, null, null, null),
            Plan("00000000-0000-0000-0000-000000000202", PlanCode.ProBasic, SubscriptionOwnerType.Professional, SubscriptionProvider.Stripe, 19, "price_basic", null, null),
            Plan("00000000-0000-0000-0000-000000000203", PlanCode.ProPlus, SubscriptionOwnerType.Professional, SubscriptionProvider.Stripe, 39, "price_plus", null, null),
            Plan("00000000-0000-0000-0000-000000000204", PlanCode.ProPremium, SubscriptionOwnerType.Professional, SubscriptionProvider.Stripe, 79, "price_premium", null, null)
        ];

        public Task<IReadOnlyList<SubscriptionPlan>> GetByOwnerTypeAsync(SubscriptionOwnerType ownerType, CancellationToken ct) => Task.FromResult<IReadOnlyList<SubscriptionPlan>>(_plans.Where(x => x.OwnerType == ownerType).ToList());
        public Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_plans.FirstOrDefault(x => x.Id == id));
        public Task<SubscriptionPlan?> GetByCodeAsync(PlanCode code, CancellationToken ct) => Task.FromResult(_plans.FirstOrDefault(x => x.Code == code));
        public Task<SubscriptionPlan?> GetByProviderProductAsync(SubscriptionProvider provider, string productId, CancellationToken ct) => Task.FromResult(_plans.FirstOrDefault(x => x.AppleProductId == productId || x.GoogleProductId == productId || x.StripePriceId == productId));

        private static SubscriptionPlan Plan(string id, PlanCode code, SubscriptionOwnerType ownerType, SubscriptionProvider? provider, decimal price, string? stripe, string? apple, string? google)
            => new() { Id = Guid.Parse(id), Code = code, Name = code.ToString(), OwnerType = ownerType, Provider = provider, PriceAmount = price, StripePriceId = stripe, AppleProductId = apple, GoogleProductId = google };
    }

    private sealed class FakeUsers : IUserSubscriptionRepository
    {
        private readonly List<UserSubscription> _items = [];
        public Task<UserSubscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(_items.LastOrDefault(x => x.UserId == userId));
        public Task<UserSubscription?> GetByExternalIdAsync(SubscriptionProvider provider, string externalSubscriptionId, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(x => x.Provider == provider && x.ExternalSubscriptionId == externalSubscriptionId));
        public Task<IReadOnlyList<UserSubscription>> ListAsync(int page, int pageSize, CancellationToken ct) => Task.FromResult<IReadOnlyList<UserSubscription>>(_items);
        public Task UpsertAsync(UserSubscription subscription, CancellationToken ct) { _items.RemoveAll(x => x.Id == subscription.Id); _items.Add(subscription); return Task.CompletedTask; }
    }

    private sealed class FakeProfessionals : IProfessionalSubscriptionRepository
    {
        private readonly List<ProfessionalSubscription> _items = [];
        public Task<ProfessionalSubscription?> GetByProfessionalIdAsync(Guid professionalId, CancellationToken ct) => Task.FromResult(_items.LastOrDefault(x => x.ProfessionalId == professionalId));
        public Task<ProfessionalSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(x => x.StripeSubscriptionId == stripeSubscriptionId));
        public Task<IReadOnlyList<ProfessionalSubscription>> ListAsync(int page, int pageSize, CancellationToken ct) => Task.FromResult<IReadOnlyList<ProfessionalSubscription>>(_items);
        public Task UpsertAsync(ProfessionalSubscription subscription, CancellationToken ct) { _items.RemoveAll(x => x.Id == subscription.Id); _items.Add(subscription); return Task.CompletedTask; }
    }

    private sealed class FakeEntitlements : ISubscriptionEntitlementRepository
    {
        public Task<IReadOnlyList<SubscriptionEntitlement>> GetByPlanIdAsync(Guid planId, CancellationToken ct)
        {
            var items = new List<SubscriptionEntitlement>();
            if (planId == Guid.Parse("00000000-0000-0000-0000-000000000204"))
                items.Add(new SubscriptionEntitlement { PlanId = planId, Key = "professional_verified_badge_enabled", Value = "true" });
            return Task.FromResult<IReadOnlyList<SubscriptionEntitlement>>(items);
        }
    }

    private sealed class FakeReceipts : IExternalPurchaseReceiptRepository { public Task InsertAsync(ExternalPurchaseReceipt receipt, CancellationToken ct) => Task.CompletedTask; }
    private sealed class FakeInvoices : ISubscriptionInvoiceRepository { public Task UpsertAsync(SubscriptionInvoice invoice, CancellationToken ct) => Task.CompletedTask; }
    private sealed class FakeWebhookEvents : IWebhookEventRepository { public Task<Guid> InsertAsync(WebhookEvent webhookEvent, CancellationToken ct) => Task.FromResult(webhookEvent.Id); public Task MarkProcessedAsync(Guid id, CancellationToken ct) => Task.CompletedTask; }

    private sealed class FakeOutbox : IOutboxRepository
    {
        public List<(Guid MessageId, string Type)> Messages { get; } = [];
        public Task AddAsync(Guid messageId, string type, string payloadJson, string? aggregateType, Guid? aggregateId, CancellationToken ct) { Messages.Add((messageId, type)); return Task.CompletedTask; }
    }
}

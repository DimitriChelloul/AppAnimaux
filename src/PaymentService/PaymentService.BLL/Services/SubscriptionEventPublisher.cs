namespace PaymentService.BLL.Services;

using System.Text.Json;
using PaymentService.DAL.Repositories;
using PaymentService.Domain.Events;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Messaging;

public sealed class SubscriptionEventPublisher
{
    private readonly IOutboxRepository _outbox;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SubscriptionEventPublisher(IOutboxRepository outbox) => _outbox = outbox;

    public Task PublishAsync<T>(string type, T evt, string aggregateType, Guid aggregateId, CancellationToken ct)
        where T : IntegrationEvent
    {
        var messageId = Guid.NewGuid();
        var env = new EventEnvelope<T>(type, EventTypes.V1, evt, DateTimeOffset.UtcNow, messageId);
        return _outbox.AddAsync(messageId, type, JsonSerializer.Serialize(env, JsonOptions), aggregateType, aggregateId, ct);
    }

    public static string EventTypeFor(SubscriptionIntegrationEvent evt) => evt switch
    {
        UserSubscriptionCreatedEvent => "UserSubscriptionCreated",
        UserSubscriptionRenewedEvent => "UserSubscriptionRenewed",
        UserSubscriptionCanceledEvent => "UserSubscriptionCanceled",
        UserSubscriptionExpiredEvent => "UserSubscriptionExpired",
        UserSubscriptionPaymentFailedEvent => "UserSubscriptionPaymentFailed",
        ProfessionalSubscriptionCreatedEvent => "ProfessionalSubscriptionCreated",
        ProfessionalSubscriptionRenewedEvent => "ProfessionalSubscriptionRenewed",
        ProfessionalSubscriptionCanceledEvent => "ProfessionalSubscriptionCanceled",
        ProfessionalSubscriptionExpiredEvent => "ProfessionalSubscriptionExpired",
        ProfessionalSubscriptionPaymentFailedEvent => "ProfessionalSubscriptionPaymentFailed",
        ProfessionalPlanChangedEvent => "ProfessionalPlanChanged",
        SubscriptionEntitlementsChangedEvent => "SubscriptionEntitlementsChanged",
        _ => "SubscriptionEvent"
    };
}

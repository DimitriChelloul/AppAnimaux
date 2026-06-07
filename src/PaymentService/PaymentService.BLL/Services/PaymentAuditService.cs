namespace PaymentService.BLL.Services;

using System.Text.Json;
using PaymentService.BLL.Interfaces;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public sealed class PaymentAuditService : IPaymentAuditService
{
    private readonly IPaymentAuditLogRepository _auditLogs;
    public PaymentAuditService(IPaymentAuditLogRepository auditLogs) => _auditLogs = auditLogs;

    public Task LogAsync(SubscriptionOwnerType ownerType, Guid ownerId, string action, SubscriptionProvider? provider, object details, CancellationToken ct)
        => _auditLogs.InsertAsync(new PaymentAuditLog
        {
            Id = Guid.NewGuid(),
            OwnerType = ownerType,
            OwnerId = ownerId,
            Action = action,
            Provider = provider,
            Details = JsonSerializer.Serialize(details)
        }, ct);
}

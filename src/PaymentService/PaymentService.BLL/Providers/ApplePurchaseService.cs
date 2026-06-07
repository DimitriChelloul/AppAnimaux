namespace PaymentService.BLL.Providers;

using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.BLL.Options;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public sealed class ApplePurchaseService : IApplePurchaseService
{
    private readonly AppleOptions _options;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly IExternalPurchaseReceiptRepository _receipts;
    private readonly IUserSubscriptionService _subscriptions;

    public ApplePurchaseService(IOptions<AppleOptions> options, ISubscriptionPlanRepository plans, IExternalPurchaseReceiptRepository receipts, IUserSubscriptionService subscriptions)
    {
        _options = options.Value;
        _plans = plans;
        _receipts = receipts;
        _subscriptions = subscriptions;
    }

    public async Task<SubscriptionStatusDto> ValidateAsync(ValidateApplePurchaseDto dto, CancellationToken ct)
    {
        if (!_options.AllowSimulatedValidation && string.IsNullOrWhiteSpace(_options.IssuerId))
        {
            throw new InvalidOperationException("Apple purchase validation is not configured.");
        }

        var plan = await _plans.GetByProviderProductAsync(SubscriptionProvider.Apple, dto.ProductId, ct)
            ?? throw new ArgumentException("Unknown Apple product.");
        var expiresAt = DateTimeOffset.UtcNow.AddMonths(1);
        var transactionId = dto.TransactionId ?? $"apple_sim_{Guid.NewGuid():N}";
        await _receipts.InsertAsync(new ExternalPurchaseReceipt
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Provider = SubscriptionProvider.Apple,
            ProductId = dto.ProductId,
            TransactionId = transactionId,
            OriginalTransactionId = dto.OriginalTransactionId ?? transactionId,
            RawReceipt = JsonSerializer.Serialize(dto),
            ValidationStatus = "valid",
            ExpiresAt = expiresAt
        }, ct);
        return await _subscriptions.CreateOrUpdateAsync(
            new CreateUserSubscriptionDto(dto.UserId, plan.Code, SubscriptionProvider.Apple),
            dto.OriginalTransactionId ?? transactionId,
            null,
            expiresAt,
            ct);
    }

    public Task<WebhookProcessResultDto> ProcessServerNotificationAsync(string payload, CancellationToken ct)
        => Task.FromResult(new WebhookProcessResultDto(true, "Apple notification stored; renewal state sync requires App Store Server API credentials."));
}

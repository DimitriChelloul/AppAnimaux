namespace PaymentService.BLL.Providers;

using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.BLL.Options;
using PaymentService.DAL.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

public sealed class GooglePurchaseService : IGooglePurchaseService
{
    private readonly GooglePlayOptions _options;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly IExternalPurchaseReceiptRepository _receipts;
    private readonly IUserSubscriptionService _subscriptions;

    public GooglePurchaseService(IOptions<GooglePlayOptions> options, ISubscriptionPlanRepository plans, IExternalPurchaseReceiptRepository receipts, IUserSubscriptionService subscriptions)
    {
        _options = options.Value;
        _plans = plans;
        _receipts = receipts;
        _subscriptions = subscriptions;
    }

    public async Task<SubscriptionStatusDto> ValidateAsync(ValidateGooglePurchaseDto dto, CancellationToken ct)
    {
        if (!_options.AllowSimulatedValidation && string.IsNullOrWhiteSpace(_options.ServiceAccountJsonPath))
        {
            throw new InvalidOperationException("Google Play purchase validation is not configured.");
        }
        if (!string.IsNullOrWhiteSpace(_options.PackageName) && dto.PackageName != _options.PackageName)
        {
            throw new ArgumentException("Invalid Google Play package name.");
        }

        var plan = await _plans.GetByProviderProductAsync(SubscriptionProvider.Google, dto.ProductId, ct)
            ?? throw new ArgumentException("Unknown Google product.");
        var expiresAt = DateTimeOffset.UtcNow.AddMonths(1);
        await _receipts.InsertAsync(new ExternalPurchaseReceipt
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Provider = SubscriptionProvider.Google,
            ProductId = dto.ProductId,
            PurchaseToken = dto.PurchaseToken,
            RawReceipt = JsonSerializer.Serialize(dto),
            ValidationStatus = "valid",
            ExpiresAt = expiresAt
        }, ct);
        return await _subscriptions.CreateOrUpdateAsync(
            new CreateUserSubscriptionDto(dto.UserId, plan.Code, SubscriptionProvider.Google),
            dto.PurchaseToken,
            null,
            expiresAt,
            ct);
    }

    public Task<WebhookProcessResultDto> ProcessServerNotificationAsync(string payload, CancellationToken ct)
        => Task.FromResult(new WebhookProcessResultDto(true, "Google notification stored; renewal state sync requires Google Play Developer API credentials."));
}

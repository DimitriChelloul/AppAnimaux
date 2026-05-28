using ProfessionalService.BLL.Models;
using ProfessionalService.DAL.Repositories;
using ProfessionalService.Domain.Entities;

namespace ProfessionalService.BLL.Services;

public sealed class ProfessionalAppService : IProfessionalAppService
{
    private readonly IProfessionalRepository _professionals;

    public ProfessionalAppService(IProfessionalRepository professionals) => _professionals = professionals;

    public Task<IReadOnlyCollection<Professional>> SearchAsync(ProfessionalSearchRequest request, CancellationToken ct)
    {
        return _professionals.SearchAsync(
            NormalizeOptional(request.Category),
            NormalizeOptional(request.City),
            request.Latitude,
            request.Longitude,
            request.RadiusKm <= 0 ? 10 : request.RadiusKm,
            request.Page,
            request.PageSize,
            ct);
    }

    public async Task<ProfessionalDetailsResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var professional = await _professionals.GetByIdAsync(id, ct);
        return professional is null ? null : await BuildDetailsAsync(professional, ct);
    }

    public async Task<ProfessionalDetailsResponse?> GetMineAsync(Guid userId, CancellationToken ct)
    {
        var professional = await _professionals.GetByUserIdAsync(userId, ct);
        return professional is null ? null : await BuildDetailsAsync(professional, ct);
    }

    public async Task<ProfessionalDetailsResponse> UpsertMineAsync(Guid userId, UpsertProfessionalRequest request, CancellationToken ct)
    {
        Validate(request);

        var existing = await _professionals.GetByUserIdAsync(userId, ct);
        var id = await _professionals.UpsertAsync(
            new Professional
            {
                UserId = userId,
                BusinessName = request.BusinessName.Trim(),
                Category = NormalizeRequired(request.Category),
                Description = NormalizeOptional(request.Description),
                Address = NormalizeOptional(request.Address),
                City = NormalizeOptional(request.City),
                PostalCode = NormalizeOptional(request.PostalCode),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Phone = NormalizeOptional(request.Phone),
                Email = NormalizeOptional(request.Email),
                Website = NormalizeOptional(request.Website),
                SubscriptionPlan = existing?.SubscriptionPlan ?? "none",
                SubscriptionStatus = existing?.SubscriptionStatus ?? "inactive"
            },
            ct);

        return await GetAsync(id, ct) ?? throw new InvalidOperationException("Professional profile could not be loaded.");
    }

    public Task<bool> DeleteMineAsync(Guid userId, Guid id, CancellationToken ct)
    {
        return _professionals.DeleteAsync(id, userId, ct);
    }

    public async Task<ProfessionalServiceItem?> AddServiceAsync(Guid userId, Guid professionalId, AddProfessionalServiceRequest request, CancellationToken ct)
    {
        if (!await IsOwnerAsync(userId, professionalId, ct))
        {
            return null;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.ServiceName);
        return await _professionals.AddServiceAsync(professionalId, request.ServiceName.Trim(), NormalizeOptional(request.Description), NormalizeOptional(request.PriceRange), request.DisplayOrder, ct);
    }

    public async Task<ProfessionalPhoto?> AddPhotoAsync(Guid userId, Guid professionalId, AddProfessionalPhotoRequest request, CancellationToken ct)
    {
        if (!await IsOwnerAsync(userId, professionalId, ct))
        {
            return null;
        }

        if (request.MediaId == Guid.Empty)
        {
            throw new ArgumentException("Media id is required.");
        }

        return await _professionals.AddPhotoAsync(professionalId, request.MediaId, request.MediaUrl, request.DisplayOrder, request.Caption, request.IsPrimary, ct);
    }

    public Task<bool> SetSubscriptionAsync(Guid professionalId, SetProfessionalSubscriptionRequest request, CancellationToken ct)
    {
        return _professionals.SetSubscriptionAsync(professionalId, NormalizePlan(request.Plan), NormalizeStatus(request.Status), ct);
    }

    public Task<bool> SetVerifiedAsync(Guid professionalId, bool isVerified, CancellationToken ct)
    {
        return _professionals.SetVerifiedAsync(professionalId, isVerified, ct);
    }

    private async Task<ProfessionalDetailsResponse> BuildDetailsAsync(Professional professional, CancellationToken ct)
    {
        var services = await _professionals.GetServicesAsync(professional.Id, ct);
        var photos = await _professionals.GetPhotosAsync(professional.Id, ct);
        return new ProfessionalDetailsResponse(professional, services, photos);
    }

    private async Task<bool> IsOwnerAsync(Guid userId, Guid professionalId, CancellationToken ct)
    {
        var professional = await _professionals.GetByIdAsync(professionalId, ct);
        return professional is not null && professional.UserId == userId;
    }

    private static void Validate(UpsertProfessionalRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BusinessName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Category);
    }

    private static string NormalizeRequired(string value) => value.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizePlan(string value)
    {
        var normalized = NormalizeRequired(value);
        return normalized switch
        {
            "none" or "professional_basic" or "professional_premium" or "professional_premium_plus" => normalized,
            "basic" => "professional_basic",
            "premium" => "professional_premium",
            "premium_plus" or "premium+" => "professional_premium_plus",
            _ => throw new ArgumentException("Invalid professional subscription plan.")
        };
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = NormalizeRequired(value);
        return normalized is "inactive" or "trialing" or "active" or "past_due" or "canceled"
            ? normalized
            : throw new ArgumentException("Invalid professional subscription status.");
    }
}

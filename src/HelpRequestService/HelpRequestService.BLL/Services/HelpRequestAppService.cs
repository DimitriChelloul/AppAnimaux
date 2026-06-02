using HelpRequestService.BLL.Models;
using HelpRequestService.DAL.Repositories;
using HelpRequestService.Domain.Entities;

namespace HelpRequestService.BLL.Services;

public sealed class HelpRequestAppService : IHelpRequestAppService
{
    private readonly IHelpRequestRepository _requests;

    public HelpRequestAppService(IHelpRequestRepository requests) => _requests = requests;

    public async Task<HelpRequestDetailsResponse> CreateAsync(Guid requesterUserId, CreateHelpRequestRequest request, CancellationToken ct)
    {
        Validate(request.Title, request.HelpType);
        var id = Guid.NewGuid();
        await _requests.InsertAsync(
            new HelpRequest
            {
                Id = id,
                RequesterUserId = requesterUserId,
                PetId = request.PetId,
                Title = request.Title.Trim(),
                Description = NormalizeOptional(request.Description),
                HelpType = NormalizeHelpType(request.HelpType),
                Status = "draft",
                City = NormalizeOptional(request.City),
                PostalCode = NormalizeOptional(request.PostalCode),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                IsPaid = request.IsPaid,
                BudgetAmount = request.BudgetAmount,
                Currency = NormalizeCurrency(request.Currency)
            },
            ct);

        return await GetAsync(id, ct) ?? throw new InvalidOperationException("Help request could not be loaded.");
    }

    public Task<IReadOnlyCollection<HelpRequest>> GetMineAsync(Guid requesterUserId, CancellationToken ct)
    {
        return _requests.GetMineAsync(requesterUserId, ct);
    }

    public Task<IReadOnlyCollection<HelpRequest>> SearchAsync(SearchHelpRequestsRequest request, CancellationToken ct)
    {
        return _requests.SearchAsync(
            NormalizeOptional(request.HelpType)?.ToLowerInvariant(),
            request.Latitude,
            request.Longitude,
            request.RadiusKm <= 0 ? 10 : request.RadiusKm,
            request.Page,
            request.PageSize,
            ct);
    }

    public async Task<HelpRequestDetailsResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var helpRequest = await _requests.GetByIdAsync(id, ct);
        if (helpRequest is null)
        {
            return null;
        }

        var offers = await _requests.GetOffersAsync(id, ct);
        return new HelpRequestDetailsResponse(helpRequest, offers);
    }

    public async Task<HelpRequestDetailsResponse?> UpdateAsync(Guid requesterUserId, Guid id, UpdateHelpRequestRequest request, CancellationToken ct)
    {
        Validate(request.Title, request.HelpType);
        var existing = await _requests.GetByIdAsync(id, ct);
        if (existing is null || existing.RequesterUserId != requesterUserId)
        {
            return null;
        }

        var updated = await _requests.UpdateAsync(
            new HelpRequest
            {
                Id = id,
                RequesterUserId = requesterUserId,
                PetId = request.PetId,
                Title = request.Title.Trim(),
                Description = NormalizeOptional(request.Description),
                HelpType = NormalizeHelpType(request.HelpType),
                City = NormalizeOptional(request.City),
                PostalCode = NormalizeOptional(request.PostalCode),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                IsPaid = request.IsPaid,
                BudgetAmount = request.BudgetAmount,
                Currency = NormalizeCurrency(request.Currency)
            },
            ct);

        return updated ? await GetAsync(id, ct) : null;
    }

    public Task<bool> PublishAsync(Guid requesterUserId, Guid id, CancellationToken ct) => _requests.SetStatusAsync(id, requesterUserId, "published", close: false, ct);

    public Task<bool> CancelAsync(Guid requesterUserId, Guid id, CancellationToken ct) => _requests.SetStatusAsync(id, requesterUserId, "cancelled", close: true, ct);

    public async Task<HelpOffer?> AddOfferAsync(Guid helperUserId, Guid helpRequestId, CreateHelpOfferRequest request, CancellationToken ct)
    {
        var helpRequest = await _requests.GetByIdAsync(helpRequestId, ct);
        if (helpRequest is null || helpRequest.Status != "published" || helpRequest.RequesterUserId == helperUserId)
        {
            return null;
        }

        return await _requests.AddOfferAsync(helpRequestId, helperUserId, NormalizeOptional(request.Message), request.ProposedAmount, NormalizeCurrency(request.Currency), ct);
    }

    public Task<HelpMatch?> AcceptOfferAsync(Guid requesterUserId, Guid helpRequestId, Guid offerId, CancellationToken ct)
    {
        return _requests.AcceptOfferAsync(requesterUserId, helpRequestId, offerId, ct);
    }

    public Task<bool> SetInProgressAsync(Guid requesterUserId, Guid id, CancellationToken ct) => _requests.SetStatusAsync(id, requesterUserId, "in_progress", close: false, ct);

    public Task<bool> CompleteAsync(Guid requesterUserId, Guid id, CancellationToken ct) => _requests.SetStatusAsync(id, requesterUserId, "completed", close: true, ct);

    private static void Validate(string title, string helpType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(helpType);
    }

    private static string NormalizeHelpType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "garde" or "petsitting" => "garde",
        "promenade" or "walk" => "promenade",
        "visite" or "home_visit" => "visite",
        "transport" or "transport_veterinaire" or "covoiturage" => "transport",
        var other => other
    };

    private static string NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? "EUR" : value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

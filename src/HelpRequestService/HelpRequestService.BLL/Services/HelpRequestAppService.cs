using HelpRequestService.BLL.Models;
using HelpRequestService.DAL.Repositories;
using HelpRequestService.Domain.Entities;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.HelpRequests;
using Shared.Contracts.Messaging;
using System.Text.Json;

namespace HelpRequestService.BLL.Services;

public sealed class HelpRequestAppService : IHelpRequestAppService
{
    private readonly IHelpRequestRepository _requests;
    private readonly IOutboxRepository _outbox;

    public HelpRequestAppService(IHelpRequestRepository requests, IOutboxRepository outbox)
    {
        _requests = requests;
        _outbox = outbox;
    }

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

        var created = await GetAsync(id, ct) ?? throw new InvalidOperationException("Help request could not be loaded.");
        await AddOutboxAsync(
            EventTypes.HelpRequests.HelpRequestCreated,
            new HelpRequestCreatedEvent
            {
                HelpRequestId = id,
                RequesterUserId = requesterUserId,
                PetId = request.PetId,
                Title = created.HelpRequest.Title,
                HelpType = created.HelpRequest.HelpType,
                City = created.HelpRequest.City,
                Status = created.HelpRequest.Status,
                SourceService = "HelpRequestService"
            },
            "help_request",
            id,
            ct);

        return created;
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

    public async Task<bool> PublishAsync(Guid requesterUserId, Guid id, CancellationToken ct)
    {
        var published = await _requests.SetStatusAsync(id, requesterUserId, "published", close: false, ct);
        if (!published)
        {
            return false;
        }

        var helpRequest = await _requests.GetByIdAsync(id, ct);
        if (helpRequest is not null)
        {
            await AddOutboxAsync(
                EventTypes.HelpRequests.HelpRequestPublished,
                new HelpRequestPublishedEvent
                {
                    HelpRequestId = helpRequest.Id,
                    RequesterUserId = helpRequest.RequesterUserId,
                    PetId = helpRequest.PetId,
                    Title = helpRequest.Title,
                    HelpType = helpRequest.HelpType,
                    City = helpRequest.City,
                    Latitude = helpRequest.Latitude,
                    Longitude = helpRequest.Longitude,
                    SourceService = "HelpRequestService"
                },
                "help_request",
                id,
                ct);
        }

        return true;
    }

    public Task<bool> CancelAsync(Guid requesterUserId, Guid id, CancellationToken ct) => _requests.SetStatusAsync(id, requesterUserId, "cancelled", close: true, ct);

    public async Task<HelpOffer?> AddOfferAsync(Guid helperUserId, Guid helpRequestId, CreateHelpOfferRequest request, CancellationToken ct)
    {
        var helpRequest = await _requests.GetByIdAsync(helpRequestId, ct);
        if (helpRequest is null || helpRequest.Status != "published" || helpRequest.RequesterUserId == helperUserId)
        {
            return null;
        }

        var offer = await _requests.AddOfferAsync(helpRequestId, helperUserId, NormalizeOptional(request.Message), request.ProposedAmount, NormalizeCurrency(request.Currency), ct);
        await AddOutboxAsync(
            EventTypes.HelpRequests.HelpOfferCreated,
            new HelpOfferCreatedEvent
            {
                HelpRequestId = helpRequestId,
                HelpOfferId = offer.Id,
                RequesterUserId = helpRequest.RequesterUserId,
                HelperUserId = helperUserId,
                Title = helpRequest.Title,
                ProposedAmount = offer.ProposedAmount,
                Currency = offer.Currency,
                SourceService = "HelpRequestService"
            },
            "help_offer",
            offer.Id,
            ct);

        return offer;
    }

    public async Task<HelpMatch?> AcceptOfferAsync(Guid requesterUserId, Guid helpRequestId, Guid offerId, CancellationToken ct)
    {
        var match = await _requests.AcceptOfferAsync(requesterUserId, helpRequestId, offerId, ct);
        if (match is null)
        {
            return null;
        }

        var helpRequest = await _requests.GetByIdAsync(helpRequestId, ct);
        await AddOutboxAsync(
            EventTypes.HelpRequests.HelpOfferAccepted,
            new HelpOfferAcceptedEvent
            {
                HelpRequestId = helpRequestId,
                HelpOfferId = offerId,
                HelpMatchId = match.Id,
                RequesterUserId = requesterUserId,
                HelperUserId = match.HelperUserId,
                Title = helpRequest?.Title ?? "",
                SourceService = "HelpRequestService"
            },
            "help_match",
            match.Id,
            ct);

        return match;
    }

    public Task<bool> SetInProgressAsync(Guid requesterUserId, Guid id, CancellationToken ct) => _requests.SetStatusAsync(id, requesterUserId, "in_progress", close: false, ct);

    public async Task<bool> CompleteAsync(Guid requesterUserId, Guid id, CancellationToken ct)
    {
        var completed = await _requests.SetStatusAsync(id, requesterUserId, "completed", close: true, ct);
        if (!completed)
        {
            return false;
        }

        var helpRequest = await _requests.GetByIdAsync(id, ct);
        await AddOutboxAsync(
            EventTypes.HelpRequests.HelpMatchCompleted,
            new HelpMatchCompletedEvent
            {
                HelpRequestId = id,
                RequesterUserId = requesterUserId,
                Title = helpRequest?.Title ?? "",
                SourceService = "HelpRequestService"
            },
            "help_request",
            id,
            ct);

        return true;
    }

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

    private async Task AddOutboxAsync<T>(string type, T data, string aggregateType, Guid aggregateId, CancellationToken ct)
        where T : IntegrationEvent
    {
        var messageId = Guid.NewGuid();
        var envelope = new EventEnvelope<T>(
            Type: type,
            Version: EventTypes.V1,
            Data: data,
            OccurredOn: DateTimeOffset.UtcNow,
            MessageId: messageId);

        var payloadJson = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _outbox.AddAsync(messageId, type, payloadJson, aggregateType, aggregateId, ct);
    }
}

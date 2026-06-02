using HelpRequestService.Domain.Entities;

namespace HelpRequestService.DAL.Repositories;

public interface IHelpRequestRepository
{
    Task<Guid> InsertAsync(HelpRequest request, CancellationToken ct);
    Task<HelpRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<HelpRequest>> GetMineAsync(Guid requesterUserId, CancellationToken ct);
    Task<IReadOnlyCollection<HelpRequest>> SearchAsync(string? helpType, double? latitude, double? longitude, double radiusKm, int page, int pageSize, CancellationToken ct);
    Task<bool> UpdateAsync(HelpRequest request, CancellationToken ct);
    Task<bool> SetStatusAsync(Guid id, Guid requesterUserId, string status, bool close, CancellationToken ct);
    Task<HelpOffer> AddOfferAsync(Guid helpRequestId, Guid helperUserId, string? message, decimal? proposedAmount, string currency, CancellationToken ct);
    Task<IReadOnlyCollection<HelpOffer>> GetOffersAsync(Guid helpRequestId, CancellationToken ct);
    Task<HelpOffer?> GetOfferAsync(Guid offerId, CancellationToken ct);
    Task<HelpMatch?> AcceptOfferAsync(Guid requesterUserId, Guid helpRequestId, Guid offerId, CancellationToken ct);
}

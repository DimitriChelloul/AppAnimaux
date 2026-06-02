using HelpRequestService.BLL.Models;
using HelpRequestService.Domain.Entities;

namespace HelpRequestService.BLL.Services;

public interface IHelpRequestAppService
{
    Task<HelpRequestDetailsResponse> CreateAsync(Guid requesterUserId, CreateHelpRequestRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<HelpRequest>> GetMineAsync(Guid requesterUserId, CancellationToken ct);
    Task<IReadOnlyCollection<HelpRequest>> SearchAsync(SearchHelpRequestsRequest request, CancellationToken ct);
    Task<HelpRequestDetailsResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<HelpRequestDetailsResponse?> UpdateAsync(Guid requesterUserId, Guid id, UpdateHelpRequestRequest request, CancellationToken ct);
    Task<bool> PublishAsync(Guid requesterUserId, Guid id, CancellationToken ct);
    Task<bool> CancelAsync(Guid requesterUserId, Guid id, CancellationToken ct);
    Task<HelpOffer?> AddOfferAsync(Guid helperUserId, Guid helpRequestId, CreateHelpOfferRequest request, CancellationToken ct);
    Task<HelpMatch?> AcceptOfferAsync(Guid requesterUserId, Guid helpRequestId, Guid offerId, CancellationToken ct);
    Task<bool> SetInProgressAsync(Guid requesterUserId, Guid id, CancellationToken ct);
    Task<bool> CompleteAsync(Guid requesterUserId, Guid id, CancellationToken ct);
}

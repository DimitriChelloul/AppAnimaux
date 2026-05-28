using ProfessionalService.BLL.Models;
using ProfessionalService.Domain.Entities;

namespace ProfessionalService.BLL.Services;

public interface IProfessionalAppService
{
    Task<IReadOnlyCollection<Professional>> SearchAsync(ProfessionalSearchRequest request, CancellationToken ct);
    Task<ProfessionalDetailsResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<ProfessionalDetailsResponse?> GetMineAsync(Guid userId, CancellationToken ct);
    Task<ProfessionalDetailsResponse> UpsertMineAsync(Guid userId, UpsertProfessionalRequest request, CancellationToken ct);
    Task<bool> DeleteMineAsync(Guid userId, Guid id, CancellationToken ct);
    Task<ProfessionalServiceItem?> AddServiceAsync(Guid userId, Guid professionalId, AddProfessionalServiceRequest request, CancellationToken ct);
    Task<ProfessionalPhoto?> AddPhotoAsync(Guid userId, Guid professionalId, AddProfessionalPhotoRequest request, CancellationToken ct);
    Task<bool> SetSubscriptionAsync(Guid professionalId, SetProfessionalSubscriptionRequest request, CancellationToken ct);
    Task<bool> SetVerifiedAsync(Guid professionalId, bool isVerified, CancellationToken ct);
}

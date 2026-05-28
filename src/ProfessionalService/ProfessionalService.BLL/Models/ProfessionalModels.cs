using ProfessionalService.Domain.Entities;

namespace ProfessionalService.BLL.Models;

public sealed record UpsertProfessionalRequest(
    string BusinessName,
    string Category,
    string? Description,
    string? Address,
    string? City,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string? Phone,
    string? Email,
    string? Website);

public sealed record ProfessionalSearchRequest(
    string? Category,
    string? City,
    double? Latitude,
    double? Longitude,
    double RadiusKm = 10,
    int Page = 1,
    int PageSize = 20);

public sealed record AddProfessionalServiceRequest(string ServiceName, string? Description, string? PriceRange, int DisplayOrder);

public sealed record AddProfessionalPhotoRequest(Guid MediaId, string? MediaUrl, int DisplayOrder, string? Caption, bool IsPrimary);

public sealed record SetProfessionalSubscriptionRequest(string Plan, string Status);

public sealed record ProfessionalDetailsResponse(
    Professional Professional,
    IReadOnlyCollection<ProfessionalServiceItem> Services,
    IReadOnlyCollection<ProfessionalPhoto> Photos);

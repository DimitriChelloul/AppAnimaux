using HelpRequestService.Domain.Entities;

namespace HelpRequestService.BLL.Models;

public sealed record CreateHelpRequestRequest(
    Guid? PetId,
    string Title,
    string? Description,
    string HelpType,
    string? City,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    bool IsPaid,
    decimal? BudgetAmount,
    string? Currency);

public sealed record UpdateHelpRequestRequest(
    Guid? PetId,
    string Title,
    string? Description,
    string HelpType,
    string? City,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    bool IsPaid,
    decimal? BudgetAmount,
    string? Currency);

public sealed record SearchHelpRequestsRequest(string? HelpType, double? Latitude, double? Longitude, double RadiusKm = 10, int Page = 1, int PageSize = 20);

public sealed record CreateHelpOfferRequest(string? Message, decimal? ProposedAmount, string? Currency);

public sealed record HelpRequestDetailsResponse(HelpRequest HelpRequest, IReadOnlyCollection<HelpOffer> Offers);

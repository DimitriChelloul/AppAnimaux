using Dapper;
using HelpRequestService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace HelpRequestService.DAL.Repositories;

public sealed class HelpRequestRepository : IHelpRequestRepository
{
    private readonly IDbConnectionFactory _db;

    public HelpRequestRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> InsertAsync(HelpRequest request, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO help_requests (
                id, requester_user_id, pet_id, title, description, help_type, status,
                city, postal_code, latitude, longitude, start_at, end_at,
                is_paid, budget_amount, currency
            )
            VALUES (
                @Id, @RequesterUserId, @PetId, @Title, @Description, @HelpType, @Status,
                @City, @PostalCode, @Latitude, @Longitude, @StartAt, @EndAt,
                @IsPaid, @BudgetAmount, @Currency
            )
            RETURNING id
            """,
            request);
    }

    public Task<HelpRequest?> GetByIdAsync(Guid id, CancellationToken ct) => GetSingleAsync("id = @Id", new { Id = id });

    public async Task<IReadOnlyCollection<HelpRequest>> GetMineAsync(Guid requesterUserId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<HelpRequest>(
            $"""
            {SelectSql}
            WHERE requester_user_id = @RequesterUserId
            ORDER BY created_at DESC
            """,
            new { RequesterUserId = requesterUserId });

        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<HelpRequest>> SearchAsync(string? helpType, double? latitude, double? longitude, double radiusKm, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var hasGeo = latitude.HasValue && longitude.HasValue;

        var rows = await cn.QueryAsync<HelpRequest>(
            $"""
            {SelectSql}
            WHERE status = 'published'
              AND (@HelpType IS NULL OR help_type = @HelpType)
              AND (
                    @HasGeo = false
                    OR (
                        latitude IS NOT NULL
                        AND longitude IS NOT NULL
                        AND (
                            6371 * acos(
                                least(
                                    1,
                                    greatest(
                                        -1,
                                        cos(radians(@Latitude)) * cos(radians(latitude)) *
                                        cos(radians(longitude) - radians(@Longitude)) +
                                        sin(radians(@Latitude)) * sin(radians(latitude))
                                    )
                                )
                            )
                        ) <= @RadiusKm
                    )
                  )
            ORDER BY start_at NULLS LAST, created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new
            {
                HelpType = string.IsNullOrWhiteSpace(helpType) ? null : helpType.Trim().ToLowerInvariant(),
                Latitude = latitude,
                Longitude = longitude,
                HasGeo = hasGeo,
                RadiusKm = radiusKm <= 0 ? 10 : radiusKm,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            });

        return rows.ToArray();
    }

    public async Task<bool> UpdateAsync(HelpRequest request, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE help_requests
            SET pet_id = @PetId,
                title = @Title,
                description = @Description,
                help_type = @HelpType,
                city = @City,
                postal_code = @PostalCode,
                latitude = @Latitude,
                longitude = @Longitude,
                start_at = @StartAt,
                end_at = @EndAt,
                is_paid = @IsPaid,
                budget_amount = @BudgetAmount,
                currency = @Currency,
                updated_at = now()
            WHERE id = @Id
              AND requester_user_id = @RequesterUserId
              AND status IN ('draft', 'published')
            """,
            request);

        return rows > 0;
    }

    public async Task<bool> SetStatusAsync(Guid id, Guid requesterUserId, string status, bool close, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE help_requests
            SET status = @Status,
                updated_at = now(),
                closed_at = CASE WHEN @Close THEN now() ELSE closed_at END
            WHERE id = @Id
              AND requester_user_id = @RequesterUserId
            """,
            new { Id = id, RequesterUserId = requesterUserId, Status = status, Close = close });

        return rows > 0;
    }

    public async Task<HelpOffer> AddOfferAsync(Guid helpRequestId, Guid helperUserId, string? message, decimal? proposedAmount, string currency, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<HelpOffer>(
            """
            INSERT INTO help_offers (help_request_id, helper_user_id, message, proposed_amount, currency)
            VALUES (@HelpRequestId, @HelperUserId, @Message, @ProposedAmount, @Currency)
            ON CONFLICT (help_request_id, helper_user_id)
            DO UPDATE SET message = EXCLUDED.message,
                          proposed_amount = EXCLUDED.proposed_amount,
                          currency = EXCLUDED.currency,
                          status = 'pending',
                          updated_at = now()
            RETURNING id AS Id, help_request_id AS HelpRequestId, helper_user_id AS HelperUserId,
                      message AS Message, proposed_amount AS ProposedAmount, currency AS Currency,
                      status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt
            """,
            new { HelpRequestId = helpRequestId, HelperUserId = helperUserId, Message = message, ProposedAmount = proposedAmount, Currency = currency });
    }

    public async Task<IReadOnlyCollection<HelpOffer>> GetOffersAsync(Guid helpRequestId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<HelpOffer>(
            """
            SELECT id AS Id, help_request_id AS HelpRequestId, helper_user_id AS HelperUserId,
                   message AS Message, proposed_amount AS ProposedAmount, currency AS Currency,
                   status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM help_offers
            WHERE help_request_id = @HelpRequestId
            ORDER BY created_at
            """,
            new { HelpRequestId = helpRequestId });

        return rows.ToArray();
    }

    public async Task<HelpOffer?> GetOfferAsync(Guid offerId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<HelpOffer>(
            """
            SELECT id AS Id, help_request_id AS HelpRequestId, helper_user_id AS HelperUserId,
                   message AS Message, proposed_amount AS ProposedAmount, currency AS Currency,
                   status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM help_offers
            WHERE id = @OfferId
            """,
            new { OfferId = offerId });
    }

    public async Task<HelpMatch?> AcceptOfferAsync(Guid requesterUserId, Guid helpRequestId, Guid offerId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        var request = await cn.QuerySingleOrDefaultAsync<HelpRequest>(
            $"{SelectSql} WHERE id = @HelpRequestId AND requester_user_id = @RequesterUserId",
            new { HelpRequestId = helpRequestId, RequesterUserId = requesterUserId },
            tx);
        if (request is null || request.Status != "published")
        {
            tx.Rollback();
            return null;
        }

        var offer = await cn.QuerySingleOrDefaultAsync<HelpOffer>(
            """
            SELECT id AS Id, help_request_id AS HelpRequestId, helper_user_id AS HelperUserId,
                   message AS Message, proposed_amount AS ProposedAmount, currency AS Currency,
                   status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM help_offers
            WHERE id = @OfferId
              AND help_request_id = @HelpRequestId
              AND status = 'pending'
            """,
            new { OfferId = offerId, HelpRequestId = helpRequestId },
            tx);
        if (offer is null)
        {
            tx.Rollback();
            return null;
        }

        await cn.ExecuteAsync("UPDATE help_offers SET status = 'rejected', updated_at = now() WHERE help_request_id = @HelpRequestId AND id <> @OfferId", new { HelpRequestId = helpRequestId, OfferId = offerId }, tx);
        await cn.ExecuteAsync("UPDATE help_offers SET status = 'accepted', updated_at = now() WHERE id = @OfferId", new { OfferId = offerId }, tx);
        await cn.ExecuteAsync("UPDATE help_requests SET status = 'accepted', updated_at = now() WHERE id = @HelpRequestId", new { HelpRequestId = helpRequestId }, tx);

        var match = await cn.QuerySingleAsync<HelpMatch>(
            """
            INSERT INTO help_matches (help_request_id, accepted_offer_id, requester_user_id, helper_user_id)
            VALUES (@HelpRequestId, @OfferId, @RequesterUserId, @HelperUserId)
            RETURNING id AS Id, help_request_id AS HelpRequestId, accepted_offer_id AS AcceptedOfferId,
                      requester_user_id AS RequesterUserId, helper_user_id AS HelperUserId,
                      status AS Status, started_at AS StartedAt, completed_at AS CompletedAt,
                      cancelled_at AS CancelledAt, cancel_reason AS CancelReason,
                      created_at AS CreatedAt, updated_at AS UpdatedAt
            """,
            new { HelpRequestId = helpRequestId, OfferId = offerId, RequesterUserId = requesterUserId, offer.HelperUserId },
            tx);

        tx.Commit();
        return match;
    }

    private async Task<HelpRequest?> GetSingleAsync(string whereClause, object parameters)
    {
        using var cn = _db.Create();
        cn.Open();
        return await cn.QuerySingleOrDefaultAsync<HelpRequest>($"{SelectSql} WHERE {whereClause}", parameters);
    }

    private const string SelectSql =
        """
        SELECT id AS Id, requester_user_id AS RequesterUserId, pet_id AS PetId,
               title AS Title, description AS Description, help_type AS HelpType,
               status AS Status, city AS City, postal_code AS PostalCode,
               latitude AS Latitude, longitude AS Longitude, start_at AS StartAt,
               end_at AS EndAt, is_paid AS IsPaid, budget_amount AS BudgetAmount,
               currency AS Currency, created_at AS CreatedAt, updated_at AS UpdatedAt,
               closed_at AS ClosedAt
        FROM help_requests
        """;
}

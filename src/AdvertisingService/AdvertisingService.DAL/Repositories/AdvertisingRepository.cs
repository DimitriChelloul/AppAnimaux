using AdvertisingService.Domain.Entities;
using Dapper;
using Shared.Persistence.Abstractions;

namespace AdvertisingService.DAL.Repositories;

public sealed class AdvertisingRepository : IAdvertisingRepository
{
    private readonly IDbConnectionFactory _db;

    public AdvertisingRepository(IDbConnectionFactory db) => _db = db;

    public async Task<AdCampaign> CreateCampaignAsync(AdCampaign campaign, AdCreative creative, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        var created = await cn.QuerySingleAsync<AdCampaign>(
            """
            INSERT INTO ad_campaigns (
                id, advertiser_user_id, name, objective, daily_budget,
                total_budget, currency, status, starts_at, ends_at,
                frequency_cap_per_user_daily, cooldown_minutes
            )
            VALUES (
                @Id, @AdvertiserUserId, @Name, @Objective, @DailyBudget,
                @TotalBudget, @Currency, @Status, @StartsAt, @EndsAt,
                @FrequencyCapPerUserDaily, @CooldownMinutes
            )
            RETURNING
                id AS Id,
                advertiser_user_id AS AdvertiserUserId,
                name AS Name,
                objective AS Objective,
                daily_budget AS DailyBudget,
                total_budget AS TotalBudget,
                currency AS Currency,
                status AS Status,
                starts_at AS StartsAt,
                ends_at AS EndsAt,
                frequency_cap_per_user_daily AS FrequencyCapPerUserDaily,
                cooldown_minutes AS CooldownMinutes,
                impressions_count AS ImpressionsCount,
                clicks_count AS ClicksCount,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            """,
            campaign,
            tx);

        await cn.ExecuteAsync(
            """
            INSERT INTO ad_creatives (
                id, campaign_id, title, body, media_url,
                landing_url, placement, weight, status
            )
            VALUES (
                @Id, @CampaignId, @Title, @Body, @MediaUrl,
                @LandingUrl, @Placement, @Weight, @Status
            )
            """,
            ToCreativeInsert(creative, created.Id),
            tx);

        tx.Commit();
        return created;
    }

    public async Task<IReadOnlyCollection<AdCampaign>> GetCampaignsAsync(Guid? advertiserUserId, string? status, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<AdCampaign>(
            $"""
            {SelectCampaignSql}
            WHERE (@AdvertiserUserId IS NULL OR advertiser_user_id = @AdvertiserUserId)
              AND (@Status IS NULL OR status = @Status)
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { AdvertiserUserId = advertiserUserId, Status = status, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task<AdCampaign?> GetCampaignAsync(Guid campaignId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<AdCampaign>(
            $"""
            {SelectCampaignSql}
            WHERE id = @CampaignId
            """,
            new { CampaignId = campaignId });
    }

    public async Task<AdPlacementResult?> GetNextAdAsync(string placement, string? viewerKey, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<AdPlacementResult>(
            """
            SELECT
                c.id AS CampaignId,
                cr.id AS CreativeId,
                cr.title AS Title,
                cr.body AS Body,
                cr.media_url AS MediaUrl,
                cr.landing_url AS LandingUrl,
                cr.placement AS Placement,
                c.frequency_cap_per_user_daily AS FrequencyCapPerUserDaily,
                c.cooldown_minutes AS CooldownMinutes
            FROM ad_creatives cr
            JOIN ad_campaigns c ON c.id = cr.campaign_id
            WHERE cr.placement = @Placement
              AND cr.status = 'active'
              AND c.status = 'active'
              AND c.starts_at <= now()
              AND (c.ends_at IS NULL OR c.ends_at >= now())
              AND (
                  @ViewerKey IS NULL
                  OR c.frequency_cap_per_user_daily IS NULL
                  OR (
                      SELECT COUNT(*)
                      FROM ad_interactions i
                      WHERE i.campaign_id = c.id
                        AND i.viewer_key = @ViewerKey
                        AND i.interaction_type = 'impression'
                        AND i.tracked_at >= date_trunc('day', now())
                  ) < c.frequency_cap_per_user_daily
              )
              AND (
                  @ViewerKey IS NULL
                  OR c.cooldown_minutes IS NULL
                  OR NOT EXISTS (
                      SELECT 1
                      FROM ad_interactions i
                      WHERE i.campaign_id = c.id
                        AND i.viewer_key = @ViewerKey
                        AND i.interaction_type = 'impression'
                        AND i.tracked_at >= now() - (c.cooldown_minutes * interval '1 minute')
                  )
              )
            ORDER BY random() * GREATEST(cr.weight, 1) DESC
            LIMIT 1
            """,
            new { Placement = placement, ViewerKey = viewerKey });
    }

    public async Task<bool> SetCampaignStatusAsync(Guid campaignId, string status, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE ad_campaigns
            SET status = @Status,
                updated_at = now()
            WHERE id = @CampaignId
            """,
            new { CampaignId = campaignId, Status = status });

        return rows > 0;
    }

    public async Task<bool> UpdateCampaignFrequencyAsync(Guid campaignId, int? frequencyCapPerUserDaily, int? cooldownMinutes, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE ad_campaigns
            SET frequency_cap_per_user_daily = @FrequencyCapPerUserDaily,
                cooldown_minutes = @CooldownMinutes,
                updated_at = now()
            WHERE id = @CampaignId
            """,
            new { CampaignId = campaignId, FrequencyCapPerUserDaily = frequencyCapPerUserDaily, CooldownMinutes = cooldownMinutes });

        return rows > 0;
    }

    public async Task<AdInteraction?> TrackAsync(AdInteraction interaction, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        var exists = await cn.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM ad_creatives cr
                JOIN ad_campaigns c ON c.id = cr.campaign_id
                WHERE cr.id = @CreativeId
                  AND c.id = @CampaignId
                  AND cr.placement = @Placement
            )
            """,
            interaction,
            tx);

        if (!exists)
        {
            tx.Rollback();
            return null;
        }

        var tracked = await cn.QuerySingleAsync<AdInteraction>(
            """
            INSERT INTO ad_interactions (
                id, campaign_id, creative_id, viewer_user_id, viewer_key,
                placement, interaction_type, landing_url, tracked_at
            )
            VALUES (
                @Id, @CampaignId, @CreativeId, @ViewerUserId, @ViewerKey,
                @Placement, @InteractionType, @LandingUrl, @TrackedAt
            )
            RETURNING
                id AS Id,
                campaign_id AS CampaignId,
                creative_id AS CreativeId,
                viewer_user_id AS ViewerUserId,
                viewer_key AS ViewerKey,
                placement AS Placement,
                interaction_type AS InteractionType,
                landing_url AS LandingUrl,
                tracked_at AS TrackedAt
            """,
            interaction,
            tx);

        var counterColumn = interaction.InteractionType == "click" ? "clicks_count" : "impressions_count";
        await cn.ExecuteAsync(
            $"""
            UPDATE ad_campaigns
            SET {counterColumn} = {counterColumn} + 1,
                updated_at = now()
            WHERE id = @CampaignId
            """,
            new { interaction.CampaignId },
            tx);

        tx.Commit();
        return tracked;
    }

    private static object ToCreativeInsert(AdCreative creative, Guid campaignId)
        => new
        {
            creative.Id,
            CampaignId = campaignId,
            creative.Title,
            creative.Body,
            creative.MediaUrl,
            creative.LandingUrl,
            creative.Placement,
            creative.Weight,
            creative.Status
        };

    private const string SelectCampaignSql =
        """
        SELECT
            id AS Id,
            advertiser_user_id AS AdvertiserUserId,
            name AS Name,
            objective AS Objective,
            daily_budget AS DailyBudget,
            total_budget AS TotalBudget,
            currency AS Currency,
            status AS Status,
            starts_at AS StartsAt,
            ends_at AS EndsAt,
            frequency_cap_per_user_daily AS FrequencyCapPerUserDaily,
            cooldown_minutes AS CooldownMinutes,
            impressions_count AS ImpressionsCount,
            clicks_count AS ClicksCount,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM ad_campaigns
        """;
}

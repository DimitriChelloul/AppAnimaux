using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Advertising;

using Shared.Contracts.Events.Abstractions;

public record AdImpressionTrackedEvent : IntegrationEvent
{
    public Guid CampaignId { get; init; }
    public Guid CreativeId { get; init; }

    public Guid? ViewerUserId { get; init; }
    public string Placement { get; init; } = default!; // home/feed/search/details

    public DateTimeOffset TrackedAt { get; init; } = DateTimeOffset.UtcNow;
}


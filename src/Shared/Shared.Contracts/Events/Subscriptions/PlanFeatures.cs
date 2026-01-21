using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Subscriptions;

public record PlanFeatures
{
    public bool AdsFree { get; init; }
    public int MonthlyCredits { get; init; }
    public int BoostsIncluded { get; init; }
    public int MaxActiveHelpRequests { get; init; }
    public bool PriorityMatching { get; init; }
    public bool ProProfile { get; init; }
}


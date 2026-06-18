using System;

namespace Darwin.Contracts.Loyalty
{
    public sealed class BusinessCampaignEntitlementResponse
    {
        public string PlanCode { get; init; } = string.Empty;
        public string PlanName { get; init; } = string.Empty;
        public bool CampaignsInAppAllowed { get; init; }
        public bool CampaignsPushAllowed { get; init; }
        public int MonthlyPushQuota { get; init; }
        public int MonthlyPushUsed { get; init; }
        public int MonthlyPushRemaining { get; init; }
        public DateTime CurrentPeriodStartUtc { get; init; }
        public DateTime CurrentPeriodEndUtc { get; init; }
    }
}

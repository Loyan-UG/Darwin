using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Campaigns
{
    /// <summary>
    /// Resolves campaign channel entitlement and usage for a business.
    /// </summary>
    public sealed class BusinessCampaignEntitlementService
    {
        public const string PushCampaignFeatureKey = "campaign.push";

        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public BusinessCampaignEntitlementService(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<BusinessCampaignEntitlementDto> GetAsync(Guid businessId, string? culture = null, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return BusinessCampaignEntitlementDto.StarterFallback(GetCurrentPeriod(_clock.UtcNow));
            }

            var period = GetCurrentPeriod(_clock.UtcNow);
            var subscription = await (from s in _db.Set<BusinessSubscription>().AsNoTracking()
                                      join p in _db.Set<BillingPlan>().AsNoTracking() on s.BillingPlanId equals p.Id
                                      where !s.IsDeleted && !p.IsDeleted && s.BusinessId == businessId
                                      orderby s.StartedAtUtc descending
                                      select new
                                      {
                                          s.Status,
                                          PlanCode = p.Code,
                                          PlanName = p.Name,
                                          PlanFeaturesJson = p.FeaturesJson
                                      })
                                     .FirstOrDefaultAsync(ct)
                                     .ConfigureAwait(false);

            var used = await GetPushUsageCountAsync(businessId, period.PeriodStartUtc, ct).ConfigureAwait(false);

            if (subscription is null)
            {
                var fallback = BusinessCampaignEntitlementDto.StarterFallback(period);
                fallback.MonthlyPushUsed = used;
                return fallback;
            }

            var features = BillingPlanFeaturesJson.Parse(subscription.PlanFeaturesJson);
            var isSubscriptionUsable = subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing;
            var pushAllowed = isSubscriptionUsable && features.CampaignsPush && features.MonthlyPushCampaigns > 0;
            var quota = pushAllowed ? features.MonthlyPushCampaigns : 0;

            return new BusinessCampaignEntitlementDto
            {
                PlanCode = subscription.PlanCode,
                PlanName = BillingLocalizedTextResolver.ResolvePlanName(subscription.PlanName, subscription.PlanFeaturesJson, culture),
                CampaignsInAppAllowed = isSubscriptionUsable && features.CampaignsInApp,
                CampaignsPushAllowed = pushAllowed,
                MonthlyPushQuota = quota,
                MonthlyPushUsed = used,
                CurrentPeriodStartUtc = period.PeriodStartUtc,
                CurrentPeriodEndUtc = period.PeriodEndUtc
            };
        }

        public async Task<int> GetPushUsageCountAsync(Guid businessId, DateTime periodStartUtc, CancellationToken ct = default)
        {
            return await _db.Set<BusinessFeatureUsage>()
                .AsNoTracking()
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.BusinessId == businessId &&
                    x.FeatureKey == PushCampaignFeatureKey &&
                    x.PeriodStartUtc == periodStartUtc, ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> HasPushUsageAsync(Guid businessId, Guid campaignId, DateTime periodStartUtc, CancellationToken ct = default)
        {
            return await _db.Set<BusinessFeatureUsage>()
                .AsNoTracking()
                .AnyAsync(x =>
                    !x.IsDeleted &&
                    x.BusinessId == businessId &&
                    x.FeatureKey == PushCampaignFeatureKey &&
                    x.PeriodStartUtc == periodStartUtc &&
                    x.SourceId == campaignId, ct)
                .ConfigureAwait(false);
        }

        public async Task RecordPushUsageAsync(Guid businessId, Guid campaignId, DateTime usedAtUtc, CancellationToken ct = default)
        {
            var period = GetCurrentPeriod(usedAtUtc);
            if (await HasPushUsageAsync(businessId, campaignId, period.PeriodStartUtc, ct).ConfigureAwait(false))
            {
                return;
            }

            _db.Set<BusinessFeatureUsage>().Add(new BusinessFeatureUsage
            {
                BusinessId = businessId,
                FeatureKey = PushCampaignFeatureKey,
                PeriodStartUtc = period.PeriodStartUtc,
                PeriodEndUtc = period.PeriodEndUtc,
                SourceId = campaignId,
                UsedAtUtc = usedAtUtc
            });
        }

        public static BusinessCampaignEntitlementPeriod GetCurrentPeriod(DateTime nowUtc)
        {
            var normalized = nowUtc.Kind == DateTimeKind.Utc ? nowUtc : DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            var start = new DateTime(normalized.Year, normalized.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return new BusinessCampaignEntitlementPeriod(start, start.AddMonths(1));
        }
    }

    public sealed class BusinessCampaignEntitlementDto
    {
        public string PlanCode { get; init; } = "starter-fallback";
        public string PlanName { get; init; } = "Starter";
        public bool CampaignsInAppAllowed { get; init; } = true;
        public bool CampaignsPushAllowed { get; init; }
        public int MonthlyPushQuota { get; init; }
        public int MonthlyPushUsed { get; set; }
        public int MonthlyPushRemaining => Math.Max(0, MonthlyPushQuota - MonthlyPushUsed);
        public DateTime CurrentPeriodStartUtc { get; init; }
        public DateTime CurrentPeriodEndUtc { get; init; }

        public static BusinessCampaignEntitlementDto StarterFallback(BusinessCampaignEntitlementPeriod period) => new()
        {
            PlanCode = "starter-fallback",
            PlanName = "Starter",
            CampaignsInAppAllowed = true,
            CampaignsPushAllowed = false,
            MonthlyPushQuota = 0,
            MonthlyPushUsed = 0,
            CurrentPeriodStartUtc = period.PeriodStartUtc,
            CurrentPeriodEndUtc = period.PeriodEndUtc
        };
    }

    public readonly record struct BusinessCampaignEntitlementPeriod(DateTime PeriodStartUtc, DateTime PeriodEndUtc);
}

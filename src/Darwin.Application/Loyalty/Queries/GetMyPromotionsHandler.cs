using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Entities.Marketing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Builds personalized promotion cards for the current user from two sources:
    /// 1) Active campaign entities (preferred source for the phase-upgrade model).
    /// 2) Derived reward-distance cards from loyalty tiers (legacy-compatible fallback).
    /// </summary>
    public sealed class GetMyPromotionsHandler
    {
        private const string ActiveCampaignState = "Active";
        private const string ScheduledCampaignState = "Scheduled";
        private const string ExpiredCampaignState = "Expired";

        private const string PointsThresholdAudience = "PointsThreshold";
        private const string JoinedMembersAudience = "JoinedMembers";
        private const string TierSegmentAudience = "TierSegment";

        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetMyPromotionsHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        public async Task<Result<MyPromotionsResultDto>> HandleAsync(MyPromotionsDto dto, CancellationToken ct = default)
        {
            if (dto is null)
            {
                return Result<MyPromotionsResultDto>.Fail("Request is required.");
            }

            var max = dto.MaxItems <= 0 ? 20 : Math.Min(dto.MaxItems, 100);
            var userId = _currentUser.GetCurrentUserId();

            var accountsQuery = _db.Set<LoyaltyAccount>()
                .AsNoTracking()
                .Where(a => a.UserId == userId && !a.IsDeleted);

            if (dto.BusinessId.HasValue && dto.BusinessId.Value != Guid.Empty)
            {
                accountsQuery = accountsQuery.Where(a => a.BusinessId == dto.BusinessId.Value);
            }

            var accounts = await accountsQuery
                .Select(a => new AccountPromotionContext(
                    a.BusinessId,
                    a.PointsBalance,
                    a.Business != null ? a.Business.Name : string.Empty))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (accounts.Count == 0)
            {
                return Result<MyPromotionsResultDto>.Ok(new MyPromotionsResultDto());
            }

            var nowUtc = DateTime.UtcNow;
            var accountLookup = accounts.ToDictionary(x => x.BusinessId);
            var joinedBusinessIds = accountLookup.Keys.ToList();
            var items = new List<PromotionFeedItemDto>(capacity: max * 2);

            // Campaign-backed cards are loaded first so the new model gradually becomes the primary signal.
            var campaignCards = await BuildCampaignCardsAsync(dto.BusinessId, nowUtc, joinedBusinessIds, accountLookup, ct)
                .ConfigureAwait(false);
            items.AddRange(campaignCards);

            // Keep legacy derived cards as fallback until campaign coverage is complete for all businesses.
            var derivedCards = await BuildDerivedCardsAsync(accounts, ct).ConfigureAwait(false);
            items.AddRange(derivedCards);

            var ordered = items
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.BusinessName)
                .ThenBy(x => x.Title)
                .Take(max)
                .ToList();

            return Result<MyPromotionsResultDto>.Ok(new MyPromotionsResultDto { Items = ordered });
        }

        /// <summary>
        /// Builds promotion cards directly from active marketing campaigns.
        /// </summary>
        private async Task<List<PromotionFeedItemDto>> BuildCampaignCardsAsync(
            Guid? businessFilter,
            DateTime nowUtc,
            IReadOnlyCollection<Guid> joinedBusinessIds,
            IReadOnlyDictionary<Guid, AccountPromotionContext> accountLookup,
            CancellationToken ct)
        {
            var campaignsQuery = _db.Set<Campaign>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Where(c => c.IsActive)
                .Where(c => (c.Channels & CampaignChannels.InApp) == CampaignChannels.InApp)
                .Where(c => (!c.StartsAtUtc.HasValue || c.StartsAtUtc.Value <= nowUtc) && (!c.EndsAtUtc.HasValue || c.EndsAtUtc.Value >= nowUtc));

            if (businessFilter.HasValue && businessFilter.Value != Guid.Empty)
            {
                var businessId = businessFilter.Value;
                campaignsQuery = campaignsQuery.Where(c => c.BusinessId == null || c.BusinessId == businessId);
            }
            else
            {
                campaignsQuery = campaignsQuery.Where(c => c.BusinessId == null || joinedBusinessIds.Contains(c.BusinessId.Value));
            }

            var campaigns = await campaignsQuery
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var cards = new List<PromotionFeedItemDto>(campaigns.Count);

            foreach (var campaign in campaigns)
            {
                var effectiveBusinessId = campaign.BusinessId ?? joinedBusinessIds.FirstOrDefault();
                if (effectiveBusinessId == Guid.Empty)
                {
                    continue;
                }

                var businessName = campaign.BusinessId.HasValue && accountLookup.TryGetValue(effectiveBusinessId, out var account)
                    ? account.BusinessName
                    : "Darwin";

                cards.Add(new PromotionFeedItemDto
                {
                    BusinessId = effectiveBusinessId,
                    BusinessName = businessName,
                    Title = string.IsNullOrWhiteSpace(campaign.Title) ? campaign.Name : campaign.Title,
                    Description = campaign.Body ?? campaign.Subtitle ?? string.Empty,
                    CtaKind = "OpenRewards",
                    Priority = 120,
                    CampaignId = campaign.Id,
                    CampaignState = ResolveCampaignState(campaign, nowUtc),
                    StartsAtUtc = campaign.StartsAtUtc,
                    EndsAtUtc = campaign.EndsAtUtc,
                    EligibilityRules = BuildEligibilityRulesFromTargeting(campaign.TargetingJson)
                });
            }

            return cards;
        }

        /// <summary>
        /// Builds legacy derived cards from loyalty reward thresholds.
        /// </summary>
        private async Task<List<PromotionFeedItemDto>> BuildDerivedCardsAsync(IReadOnlyCollection<AccountPromotionContext> accounts, CancellationToken ct)
        {
            var resultItems = new List<PromotionFeedItemDto>();

            foreach (var account in accounts)
            {
                var tiers = await (from program in _db.Set<LoyaltyProgram>().AsNoTracking()
                                   join tier in _db.Set<LoyaltyRewardTier>().AsNoTracking() on program.Id equals tier.LoyaltyProgramId
                                   where !program.IsDeleted && program.IsActive && program.BusinessId == account.BusinessId && !tier.IsDeleted
                                   orderby tier.PointsRequired
                                   select new { tier.PointsRequired, tier.Description })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                if (tiers.Count == 0)
                {
                    continue;
                }

                var redeemable = tiers
                    .Where(t => t.PointsRequired <= account.PointsBalance)
                    .OrderByDescending(t => t.PointsRequired)
                    .FirstOrDefault();

                if (redeemable is not null)
                {
                    resultItems.Add(new PromotionFeedItemDto
                    {
                        BusinessId = account.BusinessId,
                        BusinessName = account.BusinessName,
                        Title = "Reward available now",
                        Description = $"You can redeem '{redeemable.Description}' with your current points.",
                        CtaKind = "OpenRewards",
                        Priority = 100,
                        CampaignState = ActiveCampaignState,
                        EligibilityRules = new List<PromotionEligibilityRuleDto>
                        {
                            new PromotionEligibilityRuleDto
                            {
                                AudienceKind = PointsThresholdAudience,
                                MinPoints = redeemable.PointsRequired,
                                Note = "Member currently has enough points for redemption."
                            }
                        }
                    });

                    continue;
                }

                var next = tiers.FirstOrDefault(t => t.PointsRequired > account.PointsBalance);
                if (next is null)
                {
                    continue;
                }

                var missing = next.PointsRequired - account.PointsBalance;
                resultItems.Add(new PromotionFeedItemDto
                {
                    BusinessId = account.BusinessId,
                    BusinessName = account.BusinessName,
                    Title = "Close to next reward",
                    Description = $"Only {missing} points left to unlock '{next.Description}'.",
                    CtaKind = "OpenQr",
                    Priority = 80,
                    CampaignState = ActiveCampaignState,
                    EligibilityRules = new List<PromotionEligibilityRuleDto>
                    {
                        new PromotionEligibilityRuleDto
                        {
                            AudienceKind = PointsThresholdAudience,
                            MinPoints = next.PointsRequired,
                            MaxPoints = Math.Max(next.PointsRequired - 1, 0),
                            Note = $"Member needs {missing} more points to qualify."
                        }
                    }
                });
            }

            return resultItems;
        }

        /// <summary>
        /// Parses compact eligibility hints from campaign targeting JSON.
        /// </summary>
        private static List<PromotionEligibilityRuleDto> BuildEligibilityRulesFromTargeting(string? targetingJson)
        {
            var rules = new List<PromotionEligibilityRuleDto>
            {
                new PromotionEligibilityRuleDto
                {
                    AudienceKind = JoinedMembersAudience,
                    Note = "Campaign is visible to joined members."
                }
            };

            if (string.IsNullOrWhiteSpace(targetingJson))
            {
                return rules;
            }

            try
            {
                using var doc = JsonDocument.Parse(targetingJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("minPoints", out var minPointsElement) && minPointsElement.TryGetInt32(out var minPoints))
                {
                    rules.Add(new PromotionEligibilityRuleDto
                    {
                        AudienceKind = PointsThresholdAudience,
                        MinPoints = minPoints,
                        Note = "Minimum points threshold applies."
                    });
                }

                if (root.TryGetProperty("tier", out var tierElement) && tierElement.ValueKind == JsonValueKind.String)
                {
                    var tier = tierElement.GetString();
                    if (!string.IsNullOrWhiteSpace(tier))
                    {
                        rules.Add(new PromotionEligibilityRuleDto
                        {
                            AudienceKind = TierSegmentAudience,
                            TierKey = tier,
                            Note = "Campaign targets a specific member tier."
                        });
                    }
                }
            }
            catch
            {
                // Ignore malformed targeting JSON to keep feed resilient.
            }

            return rules;
        }

        /// <summary>
        /// Resolves lifecycle state for campaign cards based on active/schedule window.
        /// </summary>
        private static string ResolveCampaignState(Campaign campaign, DateTime nowUtc)
        {
            if (!campaign.IsActive)
            {
                return ExpiredCampaignState;
            }

            if (campaign.StartsAtUtc.HasValue && campaign.StartsAtUtc.Value > nowUtc)
            {
                return ScheduledCampaignState;
            }

            if (campaign.EndsAtUtc.HasValue && campaign.EndsAtUtc.Value < nowUtc)
            {
                return ExpiredCampaignState;
            }

            return ActiveCampaignState;
        }

        private sealed record AccountPromotionContext(Guid BusinessId, int PointsBalance, string BusinessName);
    }
}

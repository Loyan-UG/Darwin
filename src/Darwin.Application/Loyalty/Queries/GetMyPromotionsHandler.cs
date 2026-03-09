using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Builds personalized promotion cards for the current user from joined loyalty programs and reward tiers.
    /// </summary>
    public sealed class GetMyPromotionsHandler
    {
        private const string ActiveCampaignState = "Active";
        private const string PointsThresholdAudience = "PointsThreshold";

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

            var accounts = await (from account in accountsQuery
                                  join business in _db.Set<Business>().AsNoTracking() on account.BusinessId equals business.Id
                                  where !business.IsDeleted && business.IsActive
                                  select new
                                  {
                                      account.BusinessId,
                                      account.PointsBalance,
                                      BusinessName = business.Name
                                  })
                .ToListAsync(ct)
                .ConfigureAwait(false);

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
                if (next is not null)
                {
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
            }

            var ordered = resultItems
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.BusinessName)
                .Take(max)
                .ToList();

            return Result<MyPromotionsResultDto>.Ok(new MyPromotionsResultDto { Items = ordered });
        }
    }
}

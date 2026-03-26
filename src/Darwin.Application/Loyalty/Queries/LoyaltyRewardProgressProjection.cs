using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries;

/// <summary>
/// Shared projection helpers for member-facing loyalty reward-progress calculations.
/// </summary>
internal static class LoyaltyRewardProgressProjection
{
    /// <summary>
    /// Loads active reward thresholds grouped by business identifier.
    /// </summary>
    public static async Task<Dictionary<Guid, List<RewardThreshold>>> LoadThresholdsByBusinessAsync(
        IAppDbContext db,
        IEnumerable<Guid> businessIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(businessIds);

        var distinctBusinessIds = businessIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctBusinessIds.Count == 0)
        {
            return new Dictionary<Guid, List<RewardThreshold>>();
        }

        var rows = await (
            from program in db.Set<LoyaltyProgram>().AsNoTracking()
            join tier in db.Set<LoyaltyRewardTier>().AsNoTracking() on program.Id equals tier.LoyaltyProgramId
            where distinctBusinessIds.Contains(program.BusinessId)
                  && !program.IsDeleted
                  && program.IsActive
                  && !tier.IsDeleted
            orderby program.BusinessId, tier.PointsRequired
            select new
            {
                program.BusinessId,
                Name = !string.IsNullOrWhiteSpace(tier.Description) ? tier.Description : program.Name,
                tier.PointsRequired
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .GroupBy(x => x.BusinessId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => new RewardThreshold
                    {
                        Name = x.Name,
                        RequiredPoints = x.PointsRequired
                    })
                    .ToList());
    }

    /// <summary>
    /// Applies next-reward progress information to a member-facing account summary.
    /// </summary>
    public static void ApplyToAccount(LoyaltyAccountSummaryDto account, IReadOnlyList<RewardThreshold> thresholds)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (thresholds.Count == 0)
        {
            account.NextRewardTitle = null;
            account.NextRewardRequiredPoints = null;
            account.PointsToNextReward = null;
            account.NextRewardProgressPercent = null;
            return;
        }

        var nextThreshold = thresholds
            .Where(x => x.RequiredPoints > account.PointsBalance)
            .OrderBy(x => x.RequiredPoints)
            .FirstOrDefault();

        if (nextThreshold is null)
        {
            account.NextRewardTitle = null;
            account.NextRewardRequiredPoints = null;
            account.PointsToNextReward = 0;
            account.NextRewardProgressPercent = 100m;
            return;
        }

        account.NextRewardTitle = nextThreshold.Name;
        account.NextRewardRequiredPoints = nextThreshold.RequiredPoints;
        account.PointsToNextReward = Math.Max(nextThreshold.RequiredPoints - account.PointsBalance, 0);
        account.NextRewardProgressPercent = nextThreshold.RequiredPoints <= 0
            ? null
            : Math.Round(Math.Min(100m, (decimal)account.PointsBalance * 100m / nextThreshold.RequiredPoints), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Represents a lightweight reward threshold used for progress projections.
    /// </summary>
    internal sealed class RewardThreshold
    {
        /// <summary>Gets or sets the customer-facing reward name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the required points threshold.</summary>
        public int RequiredPoints { get; set; }
    }
}

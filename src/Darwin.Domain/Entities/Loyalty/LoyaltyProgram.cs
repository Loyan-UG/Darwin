using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Defines the loyalty rules for a business (one program per business for MVP).
    /// Supports extensible accrual modes (per-visit, amount-based) and a set of reward tiers.
    /// </summary>
    public sealed class LoyaltyProgram : BaseEntity
    {
        /// <summary>
        /// Business that owns this loyalty program.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Human-friendly name of the program as shown to customers.
        /// </summary>
        public string Name { get; set; } = "Default Loyalty Program";

        /// <summary>
        /// Accrual mode. Start with PerVisit (1 point per qualified visit). AmountBased can be activated later.
        /// </summary>
        public LoyaltyAccrualMode AccrualMode { get; set; } = LoyaltyAccrualMode.PerVisit;

        /// <summary>
        /// For amount-based accrual: number of points per currency unit (minor units based in Money, but here logical).
        /// Nullable as it's irrelevant in PerVisit mode.
        /// </summary>
        public decimal? PointsPerCurrencyUnit { get; set; }

        /// <summary>
        /// Whether this program is currently active and visible to customers in discovery.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// JSON field to store future rule expressions, blacklists, time windows, etc. Evolvable without schema churn.
        /// </summary>
        public string? RulesJson { get; set; }

        /// <summary>
        /// Navigation: reward tiers belonging to this program. Ordered by ascending threshold in UI.
        /// </summary>
        public List<LoyaltyRewardTier> RewardTiers { get; set; } = new();
    }

    
}

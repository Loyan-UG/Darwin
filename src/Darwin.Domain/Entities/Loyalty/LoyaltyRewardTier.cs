using Darwin.Domain.Common;
using Darwin.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// A single reward tier for a loyalty program (e.g., 3 points => Free coffee).
    /// </summary>
    public sealed class LoyaltyRewardTier : BaseEntity
    {
        /// <summary>
        /// Parent loyalty program.
        /// </summary>
        public Guid LoyaltyProgramId { get; set; }

        /// <summary>
        /// Points required to unlock/consume this reward tier.
        /// </summary>
        public int PointsRequired { get; set; }

        /// <summary>
        /// Reward type (e.g., FreeItem, PercentDiscount, AmountDiscount). Keep generic to fit multiple industries.
        /// </summary>
        public LoyaltyRewardType RewardType { get; set; } = LoyaltyRewardType.FreeItem;

        /// <summary>
        /// Optional numeric value for the reward type (e.g., 20 for 20% discount).
        /// </summary>
        public decimal? RewardValue { get; set; }

        /// <summary>
        /// Optional textual description displayed to customers (e.g., "Free drink").
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// If true, redemption may proceed automatically without staff approval. Otherwise, staff confirmation is required.
        /// </summary>
        public bool AllowSelfRedemption { get; set; } = false;

        /// <summary>
        /// Optional JSON metadata for POS integration (SKU, category constraints, etc.).
        /// </summary>
        public string? MetadataJson { get; set; }
    }
}

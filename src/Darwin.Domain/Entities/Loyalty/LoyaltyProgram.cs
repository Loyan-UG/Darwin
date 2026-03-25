using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Defines the loyalty rules for a business.
    /// </summary>
    public sealed class LoyaltyProgram : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = "Default Loyalty Program";
        public LoyaltyAccrualMode AccrualMode { get; set; } = LoyaltyAccrualMode.PerVisit;
        public decimal? PointsPerCurrencyUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public string? RulesJson { get; set; }
        public List<LoyaltyRewardTier> RewardTiers { get; set; } = new();

        // New-model alias.
        public bool Active
        {
            get => IsActive;
            set => IsActive = value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>Program overview displayed on business profile.</summary>
    public sealed class LoyaltyProgramSummary
    {
        public Guid LoyaltyProgramId { get; init; }
        public string Name { get; init; } = default!;
        public string AccrualMode { get; init; } = "PerVisit";
        public IReadOnlyList<LoyaltyRewardTier> RewardTiers { get; init; } = Array.Empty<LoyaltyRewardTierDto>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>Consumer-facing summary for "My Rewards" list.</summary>
    public sealed class LoyaltyAccountSummary
    {
        public Guid BusinessId { get; init; }
        public string BusinessName { get; init; } = default!;
        public int PointsBalance { get; init; }
        public DateTime? LastAccrualAtUtc { get; init; }
        public string? NextRewardTitle { get; init; }
    }
}

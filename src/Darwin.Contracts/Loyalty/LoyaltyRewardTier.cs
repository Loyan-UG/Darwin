using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    public sealed class LoyaltyRewardTier
    {
        public Guid LoyaltyRewardTierId { get; init; }
        public int Threshold { get; init; }
        public string Title { get; init; } = default!;
        public string? Description { get; init; }
    }
}

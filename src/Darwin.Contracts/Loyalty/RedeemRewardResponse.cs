using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    public sealed class RedeemRewardResponse
    {
        public int PointsSpent { get; init; }
        public int NewBalance { get; init; }
        public string RewardTitle { get; init; } = default!;
    }
}

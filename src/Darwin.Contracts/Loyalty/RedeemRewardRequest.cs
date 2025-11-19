using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>Redeems a reward tier within an active scan session.</summary>
    public sealed class RedeemRewardRequest
    {
        public Guid ScanSessionId { get; init; }
        public Guid RewardTierId { get; init; }
    }
}

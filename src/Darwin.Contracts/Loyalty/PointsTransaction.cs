using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>History entry for points ledger.</summary>
    public sealed class PointsTransaction
    {
        public DateTime OccurredAtUtc { get; init; }
        public string Type { get; init; } = "Accrual"; // Accrual/Redemption/Adjustment
        public int Delta { get; init; }
        public string? Reference { get; init; }
        public string? Notes { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>Business-facing view of the identified customer at scan time.</summary>
    public sealed class StartScanResponse
    {
        /// <summary>Ephemeral session id to be used for accrual/redemption.</summary>
        public Guid ScanSessionId { get; init; }

        /// <summary>Customer display name for the cashier to confirm verbally.</summary>
        public string CustomerDisplayName { get; init; } = default!;

        /// <summary>Current points at this business.</summary>
        public int CurrentPoints { get; init; }

        /// <summary>Next reward preview (if any).</summary>
        public string? NextRewardTitle { get; init; }

        /// <summary>Recent visits short summary (optional for UI hints).</summary>
        public string? LastVisitInfo { get; init; }
    }
}

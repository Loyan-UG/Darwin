using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>Adds points for the ongoing scan session.</summary>
    public sealed class AddPointsRequest
    {
        public Guid ScanSessionId { get; init; }

        /// <summary>Default +1 for per-visit mode.</summary>
        public int Points { get; init; } = 1;

        /// <summary>Optional reason/reference for audit.</summary>
        public string? Note { get; init; }
    }
}

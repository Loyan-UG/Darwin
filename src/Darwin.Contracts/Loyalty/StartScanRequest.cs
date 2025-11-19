using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Initiates a scan on business tablet by submitting customer's QR token.
    /// API resolves the customer and returns a scan session to proceed.
    /// </summary>
    public sealed class StartScanRequest
    {
        public string Token { get; init; } = default!;

        /// <summary>Optional: current location id of the business device.</summary>
        public Guid? BusinessLocationId { get; init; }
    }
}

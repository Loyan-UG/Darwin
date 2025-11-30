using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Represents the high-level mode of a loyalty scan session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value mirrors the server-side enum used by the Application layer
    /// and should remain stable over time to allow straightforward mapping.
    /// </para>
    /// </remarks>
    public enum LoyaltyScanMode
    {
        /// <summary>
        /// The scan session is used to accrue points (earn).
        /// </summary>
        Accrual = 0,

        /// <summary>
        /// The scan session is used to redeem one or more rewards (spend).
        /// </summary>
        Redemption = 1
    }
}

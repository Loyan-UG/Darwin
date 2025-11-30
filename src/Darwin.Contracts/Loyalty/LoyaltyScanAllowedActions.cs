using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Describes which actions are allowed for a given scan session
    /// from the perspective of the business app.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enum is intended to be used as a bitwise flags field,
    /// so that multiple actions can be enabled simultaneously.
    /// </para>
    /// </remarks>
    [Flags]
    public enum LoyaltyScanAllowedActions
    {
        /// <summary>
        /// No actions are allowed for this session.
        /// </summary>
        None = 0,

        /// <summary>
        /// The business is allowed to confirm an accrual operation
        /// (adding points for this session).
        /// </summary>
        CanConfirmAccrual = 1 << 0,

        /// <summary>
        /// The business is allowed to confirm a redemption operation
        /// (consuming one or more rewards for this session).
        /// </summary>
        CanConfirmRedemption = 1 << 1
    }
}

using System;
using Darwin.Contracts.Loyalty;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// A consumer-facing business detail payload that also includes the current user's loyalty account summary
    /// for the same business (if any).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This contract exists to support mobile "business detail" screens with a single round-trip:
    /// the UI can render the public business details and also show the current user's points/status.
    /// </para>
    /// <para>
    /// The server decides whether the business is visible/active and whether a loyalty account exists.
    /// No internal identifiers (e.g., scan-session identifiers) are exposed.
    /// </para>
    /// </remarks>
    public sealed record BusinessDetailWithMyAccount
    {
        /// <summary>
        /// Gets the public business detail model.
        /// </summary>
        public required BusinessDetail Business { get; init; }

        /// <summary>
        /// Gets a value indicating whether the current user has a loyalty account for this business.
        /// </summary>
        public required bool HasAccount { get; init; }

        /// <summary>
        /// Gets the loyalty account summary for the current user, if an account exists; otherwise null.
        /// </summary>
        public LoyaltyAccountSummary? MyAccount { get; init; }
    }

}

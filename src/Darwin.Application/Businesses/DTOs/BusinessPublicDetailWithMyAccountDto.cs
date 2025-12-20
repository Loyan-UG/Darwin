using Darwin.Application.Loyalty.DTOs;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Combines a public business detail view with the current user's loyalty account summary (if any).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This DTO is designed for consumer/mobile screens where the user opens a business details page
    /// and the UI also needs to show "my points in this business" or "join status".
    /// </para>
    /// <para>
    /// <see cref="MyAccount"/> is nullable because a user may not have an account yet for this business.
    /// Callers should use <see cref="HasAccount"/> for a convenient branch.
    /// </para>
    /// </remarks>
    public sealed class BusinessPublicDetailWithMyAccountDto
    {
        /// <summary>
        /// The public business detail. Never null when this DTO is returned.
        /// </summary>
        public BusinessPublicDetailDto Business { get; set; } = default!;

        /// <summary>
        /// Indicates whether the current user has a loyalty account for this business.
        /// </summary>
        public bool HasAccount { get; set; }

        /// <summary>
        /// The current user's loyalty account summary for this business (nullable).
        /// </summary>
        public LoyaltyAccountSummaryDto? MyAccount { get; set; }
    }
}

#nullable enable

using Darwin.Contracts.Common;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request contract for retrieving the current user's "My places" list.
    /// </summary>
    /// <remarks>
    /// This is a paged endpoint (offset paging) because the "my places" list
    /// is typically small and user-scoped.
    /// </remarks>
    public sealed class MyLoyaltyBusinessesRequest : PagedRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether inactive businesses should be included.
        /// Default is false (mobile typically hides inactive entries).
        /// </summary>
        public bool IncludeInactiveBusinesses { get; init; }
    }
}

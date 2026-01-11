using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload used by the consumer app to join (create) a loyalty account
    /// for the authenticated user in the context of the specified business.
    /// </summary>
    /// <remarks>
    /// The API route carries the authoritative business identifier as a route
    /// parameter (see POST /api/v1/loyalty/account/{businessId}/join). The body
    /// may include an optional BusinessLocationId when the user explicitly
    /// selects a specific branch.
    /// </remarks>
    public sealed class JoinLoyaltyRequest
    {
        /// <summary>
        /// Optional business location (branch) identifier.
        /// When supplied the server may use it to initialize the loyalty account
        /// with a preferred location context. This value is not used to identify
        /// the target business (the route parameter is authoritative).
        /// </summary>
        public Guid? BusinessLocationId { get; init; }
    }
}
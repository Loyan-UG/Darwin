#nullable enable

using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response contract for a single page of the unified loyalty timeline.
    /// </summary>
    public sealed class GetMyLoyaltyTimelinePageResponse
    {
        /// <summary>
        /// Gets or sets the returned items (never null).
        /// </summary>
        public IReadOnlyList<LoyaltyTimelineEntry> Items { get; init; } = Array.Empty<LoyaltyTimelineEntry>();

        /// <summary>
        /// Gets or sets the next cursor timestamp (when more data exists).
        /// Null indicates there is no next page.
        /// </summary>
        public DateTime? NextBeforeAtUtc { get; init; }

        /// <summary>
        /// Gets or sets the next cursor id (tie-breaker).
        /// Null indicates there is no next page.
        /// </summary>
        public Guid? NextBeforeId { get; init; }
    }
}

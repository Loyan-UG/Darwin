#nullable enable

using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request contract for retrieving a single page of the unified loyalty timeline
    /// using keyset cursor pagination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cursor pagination avoids heavy OFFSET paging and remains DB-agnostic.
    /// </para>
    /// <para>
    /// The cursor is represented by a tuple (BeforeAtUtc, BeforeId) to maintain stable ordering
    /// even when multiple events share the same timestamp.
    /// </para>
    /// </remarks>
    public sealed class GetMyLoyaltyTimelinePageRequest
    {
        /// <summary>
        /// Gets or sets the business identifier to filter the timeline.
        /// When null, the timeline may represent all businesses (server-defined).
        /// </summary>
        public Guid? BusinessId { get; init; }

        /// <summary>
        /// Gets or sets the maximum number of items to return.
        /// </summary>
        public int PageSize { get; init; } = 30;

        /// <summary>
        /// Gets or sets the cursor timestamp (exclusive upper bound).
        /// </summary>
        public DateTime? BeforeAtUtc { get; init; }

        /// <summary>
        /// Gets or sets the cursor id (tie-breaker for entries with same timestamp).
        /// </summary>
        public Guid? BeforeId { get; init; }
    }
}

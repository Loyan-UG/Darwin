#nullable enable

using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Represents a single entry in the unified loyalty timeline stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The timeline merges multiple underlying sources into a single, ordered stream.
    /// The server is responsible for ordering and deduplication.
    /// </para>
    /// <para>
    /// This contract must not leak internal operational identifiers such as ScanSessionId.
    /// </para>
    /// <para>
    /// IMPORTANT:
    /// This contract intentionally mirrors the Application-layer projection used by the
    /// unified timeline handler, so that WebApi remains a thin boundary (glue only).
    /// </para>
    /// </remarks>
    public sealed class LoyaltyTimelineEntry
    {
        /// <summary>
        /// Gets or sets the stable identifier of this entry within its kind.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets or sets the kind of this entry.
        /// </summary>
        public LoyaltyTimelineEntryKind Kind { get; init; }

        /// <summary>
        /// Gets or sets the loyalty account identifier associated with this entry.
        /// </summary>
        public Guid LoyaltyAccountId { get; init; }

        /// <summary>
        /// Gets or sets the business identifier associated with this entry.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the event occurred.
        /// </summary>
        public DateTime OccurredAtUtc { get; init; }

        /// <summary>
        /// Gets or sets the signed points delta when the entry represents a points transaction.
        /// For reward redemptions this value is typically null.
        /// </summary>
        public int? PointsDelta { get; init; }

        /// <summary>
        /// Gets or sets the points spent when the entry represents a reward redemption.
        /// For points transactions this value is typically null.
        /// </summary>
        public int? PointsSpent { get; init; }

        /// <summary>
        /// Gets or sets the related reward tier identifier (if applicable).
        /// When the entry is a redemption, this points to the redeemed tier.
        /// For transactions it may be null.
        /// </summary>
        public Guid? RewardTierId { get; init; }

        /// <summary>
        /// Gets or sets an optional reference token describing the origin/category of the entry.
        /// For example, for transactions this may carry values like "Accrual", "Redemption", "Adjustment".
        /// For redemptions it is typically null by design.
        /// </summary>
        public string? Reference { get; init; }

        /// <summary>
        /// Gets or sets an optional description/note (best-effort; may be null).
        /// </summary>
        public string? Note { get; init; }
    }
}

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
        /// Gets or sets the business identifier associated with this entry.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the event occurred.
        /// </summary>
        public DateTime OccurredAtUtc { get; init; }

        /// <summary>
        /// Gets or sets a stable string token describing the entry subtype.
        /// Example: "Accrual", "Redemption", "Adjustment" (for points transactions),
        /// or "Confirmed" (for redemptions) depending on server mapping.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the signed delta applied to points balance, when applicable.
        /// For reward redemptions this may be null if the server chooses to not expose it separately.
        /// </summary>
        public int? Delta { get; init; }

        /// <summary>
        /// Gets or sets an optional description/note (best-effort; may be null).
        /// </summary>
        public string? Note { get; init; }
    }
}

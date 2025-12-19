using System;
using System.Collections.Generic;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Defines the type/category of a timeline entry as presented to consumer-facing clients.
    /// This is intentionally a UI-friendly classification that aggregates multiple domain concepts
    /// into a single unified stream.
    /// </summary>
    public enum LoyaltyTimelineEntryKind
    {
        /// <summary>
        /// A points ledger transaction (accrual / redemption / adjustment).
        /// </summary>
        PointsTransaction = 0,

        /// <summary>
        /// A reward redemption record (business-side or consumer-side redemption, depending on the program rules).
        /// </summary>
        RewardRedemption = 1
    }

    /// <summary>
    /// Request DTO used by consumer/mobile applications to retrieve a unified loyalty timeline
    /// for a specific business (transactions + redemptions) using keyset paging.
    ///
    /// IMPORTANT:
    /// - This request never uses internal scan-session identifiers.
    /// - Paging is keyset-based (cursor) to keep it efficient and provider-agnostic.
    /// </summary>
    public sealed class GetMyLoyaltyTimelinePageDto
    {
        /// <summary>
        /// Gets or sets the business identifier for which the timeline should be returned.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to return.
        /// If zero or negative, a sensible default will be applied by the handler.
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets the cursor timestamp (UTC) for keyset paging.
        /// When provided, the handler returns entries strictly older than this cursor (with tie-breakers).
        /// </summary>
        public DateTime? BeforeAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the cursor identifier used as a tie-breaker for stable ordering when timestamps match.
        /// This must be provided together with <see cref="BeforeAtUtc"/> to ensure deterministic paging.
        /// </summary>
        public Guid? BeforeId { get; set; }
    }

    /// <summary>
    /// A single unified timeline entry combining multiple loyalty-related events into one stream.
    ///
    /// This model is designed to power mobile "Activity" screens where users expect a chronological list
    /// of point changes and reward usage.
    /// </summary>
    public sealed class LoyaltyTimelineEntryDto
    {
        /// <summary>
        /// Gets or sets the entry type.
        /// </summary>
        public LoyaltyTimelineEntryKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the entry identifier (transaction id or redemption id).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the owning loyalty account identifier.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the business identifier.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the logical timestamp of the entry in UTC.
        /// For now, this uses the entity creation timestamp to remain consistent with existing handlers.
        /// </summary>
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the signed points delta when <see cref="Kind"/> is <see cref="LoyaltyTimelineEntryKind.PointsTransaction"/>.
        /// Positive for accrual, negative for redemption, and either for adjustments.
        /// </summary>
        public int? PointsDelta { get; set; }

        /// <summary>
        /// Gets or sets the points spent when <see cref="Kind"/> is <see cref="LoyaltyTimelineEntryKind.RewardRedemption"/>.
        /// This is separate from <see cref="PointsDelta"/> to allow clients to show more precise labels.
        /// </summary>
        public int? PointsSpent { get; set; }

        /// <summary>
        /// Gets or sets the reward tier identifier if the entry is tied to a tier (either via transaction or redemption).
        /// </summary>
        public Guid? RewardTierId { get; set; }

        /// <summary>
        /// Gets or sets an optional reference (e.g., order number, receipt id) for transaction entries.
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Gets or sets an optional note shown to the user (mapped from transaction notes, or redemption-related note if available).
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Response DTO for a single page of unified timeline results using keyset paging.
    /// </summary>
    public sealed class LoyaltyTimelinePageDto
    {
        /// <summary>
        /// Gets or sets the returned items (newest first).
        /// Never null.
        /// </summary>
        public IReadOnlyList<LoyaltyTimelineEntryDto> Items { get; set; } = Array.Empty<LoyaltyTimelineEntryDto>();

        /// <summary>
        /// Gets or sets the cursor timestamp (UTC) for retrieving the next page.
        /// Null when there is no next page.
        /// </summary>
        public DateTime? NextBeforeAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the cursor id for retrieving the next page.
        /// Null when there is no next page.
        /// </summary>
        public Guid? NextBeforeId { get; set; }
    }
}

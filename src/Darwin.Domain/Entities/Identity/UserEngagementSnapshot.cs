using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Cached engagement snapshot per user, used for segmentation (inactive reminders),
    /// personalization, and lightweight analytics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This entity is a denormalized projection. Source-of-truth signals come from other entities/events.
    /// It should be maintained by Application logic or background workers.
    /// </para>
    /// <para>
    /// Intended uniqueness: one row per user (UserId). Enforce via unique index in Infrastructure.
    /// </para>
    /// </remarks>
    public sealed class UserEngagementSnapshot : BaseEntity
    {
        /// <summary>
        /// The user this snapshot belongs to.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Last known activity timestamp (UTC). Definition of "activity" is application-specific.
        /// </summary>
        public DateTime? LastActivityAtUtc { get; set; }

        /// <summary>
        /// Last time user authenticated (UTC), if tracked.
        /// </summary>
        public DateTime? LastLoginAtUtc { get; set; }

        /// <summary>
        /// Last loyalty-related activity timestamp (UTC), if tracked.
        /// </summary>
        public DateTime? LastLoyaltyActivityAtUtc { get; set; }

        /// <summary>
        /// Last order/purchase activity timestamp (UTC), if tracked.
        /// </summary>
        public DateTime? LastOrderAtUtc { get; set; }

        /// <summary>
        /// Aggregated event count in lifetime or a rolling window (application-defined).
        /// </summary>
        public long EventCount { get; set; }

        /// <summary>
        /// Rolling 30-day engagement score (application-defined).
        /// Keep it as a simple number for sorting/targeting.
        /// </summary>
        public int EngagementScore30d { get; set; }

        /// <summary>
        /// When the snapshot was last computed (UTC).
        /// </summary>
        public DateTime CalculatedAtUtc { get; set; }

        /// <summary>
        /// Extensible payload for future segmentation without schema changes.
        /// Example: {"inactiveDays":14,"favoriteBusinessIds":["..."]}.
        /// </summary>
        public string SnapshotJson { get; set; } = "{}";

        /// <summary>
        /// Optional navigation to the user.
        /// </summary>
        public User? User { get; private set; }
    }
}

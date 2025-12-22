#nullable enable

using System;
using Darwin.Contracts.Common;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Represents a consumer-facing "My places" entry: a business discovery card combined
    /// with the current user's loyalty account summary for that business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This contract is designed to avoid client-side orchestration (Discovery + Accounts merging).
    /// The server returns one flattened row per business the user has a loyalty account with.
    /// </para>
    /// <para>
    /// IMPORTANT: This contract must never expose internal identifiers such as UserId
    /// or any ScanSessionId. It only contains public business identifiers and safe account stats.
    /// </para>
    /// </remarks>
    public sealed class MyLoyaltyBusinessSummary
    {
        /// <summary>
        /// Gets or sets the public identifier of the business.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets or sets the business display name.
        /// </summary>
        public string BusinessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the primary category token (typically the server enum name).
        /// Example: "Cafe", "Restaurant", ...
        /// </summary>
        public string Category { get; init; } = "Unknown";

        /// <summary>
        /// Gets or sets the display city (best-effort; may be null when unknown).
        /// </summary>
        public string? City { get; init; }

        /// <summary>
        /// Gets or sets the primary coordinate for map pin display (best-effort; may be null).
        /// </summary>
        public GeoCoordinateModel? Location { get; init; }

        /// <summary>
        /// Gets or sets the primary image URL (best-effort; may be null).
        /// </summary>
        public string? PrimaryImageUrl { get; init; }

        /// <summary>
        /// Gets or sets the current spendable points balance.
        /// </summary>
        public int PointsBalance { get; init; }

        /// <summary>
        /// Gets or sets the lifetime points total (cumulative earned points).
        /// </summary>
        public int LifetimePoints { get; init; }

        /// <summary>
        /// Gets or sets the logical account status (as a stable string token).
        /// Example: "Active", "Suspended", "Closed".
        /// </summary>
        /// <remarks>
        /// The API uses a string token to keep the mobile contract stable and decoupled
        /// from server enum versioning details.
        /// </remarks>
        public string Status { get; init; } = "Active";

        /// <summary>
        /// Gets or sets the last accrual timestamp (UTC).
        /// </summary>
        public DateTime? LastAccrualAtUtc { get; init; }
    }
}

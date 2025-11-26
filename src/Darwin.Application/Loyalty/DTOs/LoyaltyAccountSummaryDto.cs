using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Represents a lightweight summary of a loyalty account for a given business
    /// from the perspective of the current user (consumer).
    /// </summary>
    /// <remarks>
    /// This DTO is optimized for "My accounts" screens in the consumer app.
    /// It intentionally does not expose internal identifiers such as UserId.
    /// </remarks>
    public sealed class LoyaltyAccountSummaryDto
    {
        /// <summary>
        /// Gets or sets the identifier of the loyalty account.
        /// The consumer app typically does not show this directly, but it is
        /// useful as a stable key when navigating to detail screens.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the business that owns this account.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets an optional human-friendly business name.
        /// This is populated by joining with the Businesses table in the query handler.
        /// </summary>
        public string? BusinessName { get; set; }

        /// <summary>
        /// Gets or sets the current spendable points balance for this account.
        /// </summary>
        public int PointsBalance { get; set; }

        /// <summary>
        /// Gets or sets the cumulative number of points ever earned by this account.
        /// This can be used to drive tier progress or show lifetime stats.
        /// </summary>
        public int LifetimePoints { get; set; }

        /// <summary>
        /// Gets or sets the logical status of the account (e.g., Active, Suspended).
        /// </summary>
        public LoyaltyAccountStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last accrual in UTC.
        /// Can be used to show "last visit" information in the UI.
        /// </summary>
        public DateTime? LastAccrualAtUtc { get; set; }
    }
}

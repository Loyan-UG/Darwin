using System;
using Darwin.Application.Common.DTOs;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Request model for listing businesses where the current user has an existing loyalty account.
    /// This is used by consumer/mobile "My places" screens.
    /// </summary>
    public sealed class MyLoyaltyBusinessListRequestDto
    {
        /// <summary>
        /// Page number (1-based).
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size for list results.
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// When true, includes businesses that are not currently active.
        /// Useful to show historical accounts for businesses that were deactivated.
        /// </summary>
        public bool IncludeInactiveBusinesses { get; set; } = true;
    }

    /// <summary>
    /// List item combining discovery-card business fields with loyalty account summary fields.
    /// This is intentionally UI-friendly for mobile list rendering, but remains business-domain oriented.
    /// </summary>
    public sealed class MyLoyaltyBusinessListItemDto
    {
        /// <summary>
        /// Business id (public and safe to expose).
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Business name.
        /// </summary>
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>
        /// Business category.
        /// </summary>
        public BusinessCategoryKind Category { get; set; }

        /// <summary>
        /// Whether the business is currently active.
        /// </summary>
        public bool IsBusinessActive { get; set; }

        /// <summary>
        /// City of the primary location (if available).
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Coordinate of the primary location (if available).
        /// </summary>
        public GeoCoordinateDto? Coordinate { get; set; }

        /// <summary>
        /// Optional URL of the primary image/logo.
        /// </summary>
        public string? PrimaryImageUrl { get; set; }

        /// <summary>
        /// Loyalty account status (Active/Suspended/...).
        /// </summary>
        public LoyaltyAccountStatus AccountStatus { get; set; }

        /// <summary>
        /// Current spendable points balance.
        /// </summary>
        public int PointsBalance { get; set; }

        /// <summary>
        /// Lifetime points ever earned.
        /// </summary>
        public int LifetimePoints { get; set; }

        /// <summary>
        /// Last accrual timestamp in UTC (if any).
        /// </summary>
        public DateTime? LastAccrualAtUtc { get; set; }
    }
}

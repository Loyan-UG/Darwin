using System;
using System.Collections.Generic;
using Darwin.Application.Common.DTOs;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Request model for public business discovery scenarios (consumer/mobile).
    /// This type is intentionally Application-level (not Contracts) to keep layering clean.
    /// </summary>
    public sealed class BusinessDiscoveryRequestDto
    {
        /// <summary>
        /// 1-based page index.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size for discovery list results.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Optional free text query (typically business name).
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// Optional city filter.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Optional business category filter.
        /// </summary>
        public BusinessCategoryKind? Category { get; set; }

        /// <summary>
        /// Optional coordinate used for proximity search.
        /// When provided, <see cref="RadiusKm"/> should also be provided.
        /// </summary>
        public GeoCoordinateDto? Coordinate { get; set; }

        /// <summary>
        /// Optional radius in kilometers used for proximity search.
        /// </summary>
        public double? RadiusKm { get; set; }
    }

    /// <summary>
    /// List item returned from public business discovery.
    /// Designed for mobile list/map cards.
    /// </summary>
    public sealed class BusinessDiscoveryListItemDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        public BusinessCategoryKind Category { get; set; }

        public bool IsActive { get; set; }

        public string? City { get; set; }

        public GeoCoordinateDto? Coordinate { get; set; }

        /// <summary>
        /// Optional URL of a primary image/logo.
        /// </summary>
        public string? PrimaryImageUrl { get; set; }

        /// <summary>
        /// Distance to the query coordinate in kilometers (only when coordinate search is used).
        /// </summary>
        public double? DistanceKm { get; set; }

        /// <summary>
        /// Optional flag indicating whether the business is open "now" at the snapshot time.
        /// Calculated at API/UI level if needed; Application does not enforce an opening-hours schema yet.
        /// </summary>
        public bool? IsOpenNow { get; set; }
    }

    /// <summary>
    /// Public business location model for consumer/mobile usage.
    /// </summary>
    public sealed class BusinessPublicLocationDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? Region { get; set; }

        public string? CountryCode { get; set; }

        public string? PostalCode { get; set; }

        public GeoCoordinateDto? Coordinate { get; set; }

        public bool IsPrimary { get; set; }

        public string? OpeningHoursJson { get; set; }
    }

    /// <summary>
    /// Public business detail model used by consumer/mobile.
    /// </summary>
    public sealed class BusinessPublicDetailDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? ContactEmail { get; set; }

        public string? ContactPhoneE164 { get; set; }

        public BusinessCategoryKind Category { get; set; }

        public bool IsActive { get; set; }

        public string DefaultCurrency { get; set; } = string.Empty;

        public string DefaultCulture { get; set; } = string.Empty;

        public string? PrimaryImageUrl { get; set; }

        public List<string> GalleryImageUrls { get; set; } = new();

        public List<BusinessPublicLocationDto> Locations { get; set; } = new();

        public LoyaltyProgramPublicDto? LoyaltyProgram { get; set; }
    }

    /// <summary>
    /// Public view of a loyalty program for consumer/mobile discovery.
    /// Only contains information that is safe and relevant to display publicly.
    /// </summary>
    public sealed class LoyaltyProgramPublicDto
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public List<LoyaltyRewardTierPublicDto> RewardTiers { get; set; } = new();
    }

    /// <summary>
    /// Public view of a loyalty reward tier for consumer/mobile.
    /// </summary>
    public sealed class LoyaltyRewardTierPublicDto
    {
        public Guid Id { get; set; }

        public int PointsRequired { get; set; }

        public LoyaltyRewardType RewardType { get; set; }

        public decimal? RewardValue { get; set; }

        public string? Description { get; set; }

        public bool AllowSelfRedemption { get; set; }
    }
}

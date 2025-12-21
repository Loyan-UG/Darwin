#nullable enable

using System;
using System.Collections.Generic;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Represents the public business detail view returned to consumer/mobile clients.
    /// This contract is intentionally "API-first" and should be stable for mobile parsing.
    /// </summary>
    public sealed class BusinessDetail
    {
        /// <summary>
        /// Gets or sets the public identifier of the business.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the business.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the business category kind as a string token (enum name).
        /// Example: "Cafe", "Restaurant", ...
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a short description intended for mobile UI cards and headers.
        /// This maps to Application's BusinessPublicDetailDto.ShortDescription.
        /// </summary>
        public string? ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets a longer description (if/when available).
        /// For now this may be null; consumers should prefer <see cref="ShortDescription"/>.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the primary image URL (logo/cover).
        /// This maps to Application's BusinessPublicDetailDto.PrimaryImageUrl.
        /// </summary>
        public string? PrimaryImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the gallery image URLs (non-primary).
        /// This maps to Application's BusinessPublicDetailDto.GalleryImageUrls.
        /// </summary>
        public IReadOnlyList<string>? GalleryImageUrls { get; set; }

        /// <summary>
        /// Gets or sets a legacy combined image list. This is kept for backward compatibility.
        /// New clients should use <see cref="PrimaryImageUrl"/> and <see cref="GalleryImageUrls"/>.
        /// </summary>
        public IReadOnlyList<string>? ImageUrls { get; set; }

        /// <summary>
        /// Gets or sets the display city for list/detail headers.
        /// This is typically derived from the primary location.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the display coordinate for map previews.
        /// This is typically derived from the primary location.
        /// </summary>
        public GeoCoordinateModel? Coordinate { get; set; }

        /// <summary>
        /// Gets or sets the opening hours payload if/when standardized.
        /// </summary>
        public object? OpeningHours { get; set; }

        /// <summary>
        /// Gets or sets the phone number in E.164 format if available.
        /// </summary>
        public string? PhoneE164 { get; set; }

        /// <summary>
        /// Gets or sets the default currency (e.g. "EUR").
        /// </summary>
        public string DefaultCurrency { get; set; } = "EUR";

        /// <summary>
        /// Gets or sets the default culture (e.g. "de-DE").
        /// </summary>
        public string DefaultCulture { get; set; } = "de-DE";

        /// <summary>
        /// Gets or sets the website URL.
        /// </summary>
        public string? WebsiteUrl { get; set; }

        /// <summary>
        /// Gets or sets the contact email.
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Gets or sets the contact phone number in E.164 format.
        /// </summary>
        public string? ContactPhoneE164 { get; set; }

        /// <summary>
        /// Gets or sets the public locations belonging to this business.
        /// </summary>
        public List<BusinessLocation> Locations { get; set; } = new();

        /// <summary>
        /// Gets or sets the legacy loyalty program summary shape (if present).
        /// Kept for backward compatibility.
        /// </summary>
        public object? LoyaltyProgram { get; set; }

        /// <summary>
        /// Gets or sets the public loyalty program view designed for discovery/detail screens.
        /// This maps to Application's LoyaltyProgramPublicDto.
        /// </summary>
        public LoyaltyProgramPublic? LoyaltyProgramPublic { get; set; }
    }
}

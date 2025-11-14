using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents a physical or virtual location for a business.
    /// A business can maintain multiple branches; one can be marked as primary.
    /// </summary>
    public sealed class BusinessLocation : BaseEntity
    {
        /// <summary>
        /// FK to the owning business.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Friendly location name for internal/admin use (e.g., "Downtown Branch", "Hall 1").
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Street address line 1 (e.g., "Hauptstraße 1").
        /// </summary>
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// Street address line 2 (optional, suite/floor/building).
        /// </summary>
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// City/locality (e.g., "Northeim").
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// State/region (e.g., "NI" for Niedersachsen). Keep free-form for internationalization.
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// ISO 3166-1 alpha-2 country code (e.g., "DE").
        /// </summary>
        public string? CountryCode { get; set; }

        /// <summary>
        /// Postal/ZIP code (string to preserve formatting and leading zeros).
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Optional geocoordinate to enable map discovery, proximity search, and directions.
        /// </summary>
        public GeoCoordinate? Coordinate { get; set; }

        /// <summary>
        /// True when this is the primary branch for display and default operations.
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        /// <summary>
        /// Optional opening hours in a normalized JSON model (e.g., per weekday with intervals).
        /// Validation and shape are enforced at application/API layer.
        /// </summary>
        public string? OpeningHoursJson { get; set; }

        /// <summary>
        /// Optional free-form note for staff (not displayed publicly).
        /// </summary>
        public string? InternalNote { get; set; }
    }
}

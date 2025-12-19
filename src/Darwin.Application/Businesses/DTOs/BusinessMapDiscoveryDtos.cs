using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Represents a rectangular viewport in WGS84 coordinates.
    /// Mobile map UIs typically query businesses within the currently visible bounds.
    /// </summary>
    public sealed class GeoBoundsDto
    {
        /// <summary>North latitude (max latitude).</summary>
        public double NorthLat { get; set; }

        /// <summary>South latitude (min latitude).</summary>
        public double SouthLat { get; set; }

        /// <summary>East longitude (max longitude).</summary>
        public double EastLon { get; set; }

        /// <summary>West longitude (min longitude).</summary>
        public double WestLon { get; set; }
    }

    /// <summary>
    /// Request model for map discovery (pins) where the client provides the current viewport bounds.
    /// </summary>
    public sealed class BusinessMapDiscoveryRequestDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 200;

        /// <summary>
        /// The current map bounds (viewport) in which businesses should be returned.
        /// </summary>
        public GeoBoundsDto Bounds { get; set; } = new();

        public string? Query { get; set; }

        public string? CountryCode { get; set; }

        public BusinessCategoryKind? Category { get; set; }
    }
}

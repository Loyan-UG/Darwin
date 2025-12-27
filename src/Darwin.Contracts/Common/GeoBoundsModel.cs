namespace Darwin.Contracts.Common
{
    /// <summary>
    /// Represents a rectangular viewport (bounding box) in WGS84 coordinates.
    /// Used by map UIs to query entities that intersect with the currently visible region.
    /// </summary>
    public sealed class GeoBoundsModel
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
}

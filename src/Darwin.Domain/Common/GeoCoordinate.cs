namespace Darwin.Domain.Common
{
    /// <summary>
    /// Represents a geographic coordinate in WGS84 (EPSG:4326).
    /// Immutable value object used for geospatial features such as business locations and map discovery.
    /// </summary>
    public sealed class GeoCoordinate
    {
        /// <summary>
        /// Latitude in decimal degrees. Valid range: [-90.0, 90.0].
        /// North is positive, South is negative.
        /// </summary>
        public double Latitude { get; }

        /// <summary>
        /// Longitude in decimal degrees. Valid range: [-180.0, 180.0].
        /// East is positive, West is negative.
        /// </summary>
        public double Longitude { get; }

        /// <summary>
        /// Optional altitude in meters above mean sea level. Use null when not applicable.
        /// </summary>
        public double? AltitudeMeters { get; }

        /// <summary>
        /// Creates a new immutable geocoordinate instance.
        /// Throws <see cref="ArgumentOutOfRangeException"/> if inputs are out of valid ranges.
        /// </summary>
        public GeoCoordinate(double latitude, double longitude, double? altitudeMeters = null)
        {
            if (latitude < -90d || latitude > 90d)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be within [-90, 90].");
            if (longitude < -180d || longitude > 180d)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be within [-180, 180].");

            Latitude = latitude;
            Longitude = longitude;
            AltitudeMeters = altitudeMeters;
        }

        /// <summary>
        /// Returns a user-friendly "lat,lon" string (no altitude).
        /// </summary>
        public override string ToString() => $"{Latitude},{Longitude}";
    }
}

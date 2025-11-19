namespace Darwin.Contracts.Common;

/// <summary>
/// Represents a geographic coordinate in WGS84 (EPSG:4326) for API contracts.
/// Mirrors the GeoCoordinate value object in the domain and can be reused
/// by multiple feature areas (business discovery, delivery radius, etc.).
/// </summary>
public sealed class GeoCoordinate
{
    /// <summary>
    /// Gets the latitude in decimal degrees. Valid range: -90..+90.
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Gets the longitude in decimal degrees. Valid range: -180..+180.
    /// </summary>
    public double Longitude { get; init; }

    /// <summary>
    /// Gets the optional altitude in meters above sea level.
    /// This is rarely needed for mobile use-cases but is included
    /// to keep the contract aligned with the domain value object.
    /// </summary>
    public double? AltitudeMeters { get; init; }
}

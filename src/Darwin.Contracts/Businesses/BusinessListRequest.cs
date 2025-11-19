using Darwin.Contracts.Common;

namespace Darwin.Contracts.Businesses;

/// <summary>
/// Discovery filter for business listing.
/// </summary>
public sealed class BusinessListRequest : PagedRequest
{
    /// <summary>
    /// Optional category filter, e.g., "Cafe", "Restaurant".
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Optional free-text city filter.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Optional proximity filter.
    /// </summary>
    public GeoCoordinate? Near { get; init; }

    /// <summary>
    /// Optional radius in meters when Near is set (default 3000).
    /// </summary>
    public int? RadiusMeters { get; init; }
}

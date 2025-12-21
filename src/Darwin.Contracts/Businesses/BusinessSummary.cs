using System;
using Darwin.Contracts.Common;

namespace Darwin.Contracts.Businesses;

/// <summary>Business list item for discovery pages.</summary>
public sealed class BusinessSummary
{
    /// <summary>Business id.</summary>
    public Guid Id { get; init; }

    /// <summary>Business name.</summary>
    public string Name { get; init; } = default!;
    public string? ShortDescription { get; init; }
    public string? LogoUrl { get; init; }

    /// <summary>Primary category (e.g., "Cafe", "Restaurant").</summary>
    public string Category { get; init; } = "Unknown";

    /// <summary>Optional rating average 0..5.</summary>
    public double? Rating { get; init; }
    public int? RatingCount { get; init; }

    /// <summary>Coordinates for map pin.</summary>
    public GeoCoordinateModel? Location { get; init; }

    /// <summary>City or locality for quick display.</summary>
    public string? City { get; init; }

    /// <summary>True if open at the time of the query snapshot.</summary>
    public bool? IsOpenNow { get; init; }

    /// <summary>True if the business is currently active.</summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets or sets the distance from the query coordinate in meters (when proximity search is used).
    /// This is derived from Application's DistanceKm.
    /// </summary>
    public int? DistanceMeters { get; set; }
}
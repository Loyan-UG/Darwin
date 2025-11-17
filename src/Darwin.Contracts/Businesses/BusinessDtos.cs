using System;
using Darwin.Contracts.Common;

namespace Darwin.Contracts.Businesses;

/// <summary>Business list item for discovery pages.</summary>
public sealed class BusinessSummaryDto
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
    public GeoCoordinateDto? Location { get; init; }

    /// <summary>City or locality for quick display.</summary>
    public string? City { get; init; }

    /// <summary>True if open at the time of the query snapshot.</summary>
    public bool? IsOpenNow { get; init; }

    /// <summary>True if the business is currently active.</summary>
    public bool IsActive { get; init; }
}

/// <summary>Detailed business profile including locations and loyalty overview.</summary>
public sealed class BusinessDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? Description { get; init; }
    /// <summary>Multiple photos or branding images.</summary>
    public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();

    /// <summary>Structured opening hours string (client may parse).</summary>
    public string? OpeningHours { get; init; }

    /// <summary>Phone number in E.164 if present.</summary>
    public string? PhoneE164 { get; init; }
    public string DefaultCurrency { get; init; } = "EUR";
    public string DefaultCulture { get; init; } = "de-DE";
    public string? WebsiteUrl { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhoneE164 { get; init; }
    public IReadOnlyList<BusinessLocationDto> Locations { get; init; } = Array.Empty<BusinessLocationDto>();
    public Loyalty.LoyaltyProgramSummaryDto? LoyaltyProgram { get; init; }
}

/// <summary>Physical/virtual branch for a business.</summary>
public sealed class BusinessLocationDto
{
    public Guid BusinessLocationId { get; init; }
    public string Name { get; init; } = default!;
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? CountryCode { get; init; }
    public string? PostalCode { get; init; }
    public GeoCoordinateDto? Coordinate { get; init; }
    public bool IsPrimary { get; init; }
    public string? OpeningHoursJson { get; init; }
}

/// <summary>Simple lat/lng for maps and proximity.</summary>
public sealed class GeoCoordinateDto
{
    /// <summary>Latitude in decimal degrees.</summary>
    public double Latitude { get; init; }

    /// <summary>Longitude in decimal degrees.</summary>
    public double Longitude { get; init; }
}

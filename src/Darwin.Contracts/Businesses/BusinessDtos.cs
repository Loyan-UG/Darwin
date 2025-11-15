using System;
using Darwin.Contracts.Common;

namespace Darwin.Contracts.Businesses;

/// <summary>Business list item for discovery pages.</summary>
public sealed class BusinessSummaryDto
{
    public Guid BusinessId { get; init; }
    public string Name { get; init; } = default!;
    public string? ShortDescription { get; init; }
    public string? LogoUrl { get; init; }
    public string Category { get; init; } = "Unknown";
    public decimal? Rating { get; init; }
    public int? RatingCount { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>Detailed business profile including locations and loyalty overview.</summary>
public sealed class BusinessDetailDto
{
    public Guid BusinessId { get; init; }
    public string Name { get; init; } = default!;
    public string? ShortDescription { get; init; }
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
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

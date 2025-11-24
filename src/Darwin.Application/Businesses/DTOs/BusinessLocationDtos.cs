using Darwin.Application.Common.DTOs;
using System;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// DTO for creating a business location.
    /// </summary>
    public sealed class BusinessLocationCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = default!;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? CountryCode { get; set; }
        public string? PostalCode { get; set; }
        public GeoCoordinateDto? Coordinate { get; set; }
        public bool IsPrimary { get; set; }
        public string? OpeningHoursJson { get; set; }
        public string? InternalNote { get; set; }
    }

    /// <summary>
    /// DTO for editing a business location.
    /// </summary>
    public sealed class BusinessLocationEditDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = default!;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? CountryCode { get; set; }
        public string? PostalCode { get; set; }
        public GeoCoordinateDto? Coordinate { get; set; }
        public bool IsPrimary { get; set; }
        public string? OpeningHoursJson { get; set; }
        public string? InternalNote { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO for soft deleting a location.
    /// </summary>
    public sealed class BusinessLocationDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Lightweight location list row for grids.
    /// </summary>
    public sealed class BusinessLocationListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = default!;
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? CountryCode { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

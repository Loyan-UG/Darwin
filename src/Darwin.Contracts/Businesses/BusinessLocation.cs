using Darwin.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Physical/virtual branch for a business.
    /// </summary>
    public sealed class BusinessLocation
    {
        public Guid BusinessLocationId { get; init; }
        public string Name { get; init; } = default!;
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? City { get; init; }
        public string? Region { get; init; }
        public string? CountryCode { get; init; }
        public string? PostalCode { get; init; }
        public GeoCoordinate? Coordinate { get; init; }
        public bool IsPrimary { get; init; }
        public string? OpeningHoursJson { get; init; }
    }
}

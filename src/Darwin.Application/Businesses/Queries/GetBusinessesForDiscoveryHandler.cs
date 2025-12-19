using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Provides a public business discovery list for consumer/mobile scenarios.
    /// Supports filtering by query text, address/city, category and optional proximity.
    /// </summary>
    /// <remarks>
    /// This handler intentionally avoids any dependency on Contracts types.
    /// The WebApi layer may map its request/response contracts to these DTOs.
    ///
    /// Geo strategy:
    /// - Apply a database-agnostic bounding box filter in SQL for proximity queries.
    /// - Compute the precise distance using Haversine in memory for sorting and client display.
    /// This avoids hard-coupling to a specific DB vendor's spatial extensions.
    /// </remarks>
    public sealed class GetBusinessesForDiscoveryHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessesForDiscoveryHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Returns a paged list of businesses suitable for discovery screens.
        /// </summary>
        public async Task<(List<BusinessDiscoveryListItemDto> Items, int Total)> HandleAsync(
            BusinessDiscoveryRequestDto request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            // Base business query: discovery shows only active businesses.
            var businessQuery = _db.Set<Business>()
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (request.Category.HasValue)
            {
                var cat = request.Category.Value;
                businessQuery = businessQuery.Where(x => x.Category == cat);
            }

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var q = request.Query.Trim();
                businessQuery = businessQuery.Where(x =>
                    x.Name.Contains(q) ||
                    (x.ShortDescription != null && x.ShortDescription.Contains(q)));
            }

            // Primary location acts as display location for discovery cards.
            var primaryLocationQuery = _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(l => l.IsPrimary);

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim();
                primaryLocationQuery = primaryLocationQuery.Where(l =>
                    l.City != null && l.City.Contains(city));
            }

            if (!string.IsNullOrWhiteSpace(request.CountryCode))
            {
                var cc = request.CountryCode.Trim();
                primaryLocationQuery = primaryLocationQuery.Where(l =>
                    l.CountryCode != null && l.CountryCode == cc);
            }

            if (!string.IsNullOrWhiteSpace(request.AddressQuery))
            {
                var aq = request.AddressQuery.Trim();

                // Match against common address fields and location name to support partial address search.
                primaryLocationQuery = primaryLocationQuery.Where(l =>
                    (l.Name != null && l.Name.Contains(aq)) ||
                    (l.AddressLine1 != null && l.AddressLine1.Contains(aq)) ||
                    (l.AddressLine2 != null && l.AddressLine2.Contains(aq)) ||
                    (l.City != null && l.City.Contains(aq)) ||
                    (l.Region != null && l.Region.Contains(aq)) ||
                    (l.PostalCode != null && l.PostalCode.Contains(aq)) ||
                    (l.CountryCode != null && l.CountryCode.Contains(aq)));
            }

            // Optional proximity: apply a DB-agnostic bounding box filter in SQL.
            // This reduces result set size before paging and keeps us portable across DB engines.
            if (request.Coordinate != null && request.RadiusKm.HasValue && request.RadiusKm.Value > 0)
            {
                ApplyBoundingBoxFilter(primaryLocationQuery, request.Coordinate, request.RadiusKm.Value, out primaryLocationQuery);
            }

            // Compose final projection.
            var composed =
                from b in businessQuery
                join l in primaryLocationQuery
                    on b.Id equals l.BusinessId into lg
                from l in lg.DefaultIfEmpty()
                select new BusinessDiscoveryListItemDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    ShortDescription = b.ShortDescription,
                    Category = b.Category,
                    IsActive = b.IsActive,
                    City = l != null ? l.City : null,
                    Coordinate = l != null && l.Coordinate != null
                        ? new GeoCoordinateDto
                        {
                            Latitude = l.Coordinate.Latitude,
                            Longitude = l.Coordinate.Longitude,
                            AltitudeMeters = l.Coordinate.AltitudeMeters
                        }
                        : null,
                    PrimaryImageUrl = _db.Set<BusinessMedia>()
                        .AsNoTracking()
                        .Where(m => m.BusinessId == b.Id && m.IsPrimary)
                        .OrderBy(m => m.SortOrder)
                        .Select(m => m.Url)
                        .FirstOrDefault()
                };

            var total = await composed.CountAsync(ct).ConfigureAwait(false);

            // Page in SQL first; distance calc is done on the page results.
            var pageItems = await composed
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Compute distance for the page if requested.
            if (request.Coordinate != null && request.RadiusKm.HasValue && request.RadiusKm.Value > 0)
            {
                var origin = request.Coordinate;
                var radiusKm = request.RadiusKm.Value;

                foreach (var item in pageItems)
                {
                    if (item.Coordinate == null)
                    {
                        item.DistanceKm = null;
                        continue;
                    }

                    item.DistanceKm = HaversineKm(origin, item.Coordinate);
                }

                // Keep only items inside the radius and order them by distance.
                pageItems = pageItems
                    .Where(x => x.DistanceKm.HasValue && x.DistanceKm.Value <= radiusKm)
                    .OrderBy(x => x.DistanceKm)
                    .ThenBy(x => x.Name)
                    .ToList();
            }

            return (pageItems, total);
        }

        /// <summary>
        /// Applies a bounding box filter (lat/lon ranges) on the location query.
        /// This is a database-agnostic approach that works without spatial extensions.
        /// </summary>
        private static void ApplyBoundingBoxFilter(
            IQueryable<BusinessLocation> source,
            GeoCoordinateDto origin,
            double radiusKm,
            out IQueryable<BusinessLocation> filtered)
        {
            // Rough conversions:
            // - 1 degree latitude is ~111 km.
            // - 1 degree longitude varies with latitude (~111 km * cos(lat)).
            const double kmPerDegreeLat = 111.0;

            var lat = origin.Latitude;
            var lon = origin.Longitude;

            var latDelta = radiusKm / kmPerDegreeLat;

            // Prevent division by near-zero cos(lat) close to the poles.
            var cosLat = Math.Cos(lat * (Math.PI / 180.0));
            if (Math.Abs(cosLat) < 0.000001)
                cosLat = 0.000001;

            var lonDelta = radiusKm / (kmPerDegreeLat * cosLat);

            var minLat = lat - latDelta;
            var maxLat = lat + latDelta;
            var minLon = lon - lonDelta;
            var maxLon = lon + lonDelta;

            // Important: coordinate is optional.
            filtered = source.Where(l =>
                l.Coordinate != null &&
                l.Coordinate.Latitude >= minLat && l.Coordinate.Latitude <= maxLat &&
                l.Coordinate.Longitude >= minLon && l.Coordinate.Longitude <= maxLon);
        }

        /// <summary>
        /// Computes great-circle distance between two coordinates using the Haversine formula.
        /// </summary>
        private static double HaversineKm(GeoCoordinateDto a, GeoCoordinateDto b)
        {
            // No nulls are possible here; caller ensures both objects exist.
            const double r = 6371.0; // Earth radius in km

            static double ToRad(double deg) => deg * (Math.PI / 180.0);

            var dLat = ToRad(b.Latitude - a.Latitude);
            var dLon = ToRad(b.Longitude - a.Longitude);

            var lat1 = ToRad(a.Latitude);
            var lat2 = ToRad(b.Latitude);

            var sin1 = Math.Sin(dLat / 2);
            var sin2 = Math.Sin(dLon / 2);

            var h = (sin1 * sin1) + (Math.Cos(lat1) * Math.Cos(lat2) * sin2 * sin2);
            var c = 2 * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));

            return r * c;
        }
    }
}

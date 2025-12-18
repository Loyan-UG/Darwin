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
    /// Supports filtering by query text, city, category and optional proximity.
    /// </summary>
    /// <remarks>
    /// This handler intentionally avoids any dependency on Contracts types.
    /// The WebApi layer may map its request/response contracts to these DTOs.
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

            // We use the primary location as a "display" location for discovery cards.
            // If a business does not have a primary location, we still allow it to show up.
            var primaryLocationQuery = _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(l => l.IsPrimary);

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim();
                primaryLocationQuery = primaryLocationQuery.Where(l =>
                    l.City != null && l.City.Contains(city));
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

            // For proximity search we load a bounded page and compute distance in-memory.
            // This keeps EF queries predictable without requiring spatial DB dependencies.
            var pageItems = await composed
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

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

                    var d = HaversineKm(origin, item.Coordinate);
                    item.DistanceKm = d;
                }

                pageItems = pageItems
                    .Where(x => x.DistanceKm.HasValue && x.DistanceKm.Value <= radiusKm)
                    .OrderBy(x => x.DistanceKm)
                    .ThenBy(x => x.Name)
                    .ToList();
            }

            return (pageItems, total);
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

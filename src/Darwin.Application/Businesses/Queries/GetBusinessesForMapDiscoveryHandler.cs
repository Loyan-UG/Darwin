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
    /// Returns businesses inside a given map viewport (bounds) for pin rendering.
    /// This query is DB-agnostic and avoids spatial extensions by using lat/lon range filters.
    /// </summary>
    public sealed class GetBusinessesForMapDiscoveryHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessesForMapDiscoveryHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Returns a paged list of businesses within the provided bounds.
        /// </summary>
        public async Task<(List<BusinessDiscoveryListItemDto> Items, int Total)> HandleAsync(
            BusinessMapDiscoveryRequestDto request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Bounds);

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 200 : request.PageSize;

            // Base: active businesses only.
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

            var b = request.Bounds;

            // Primary location query with bounds.
            var primaryLocationQuery = _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(l =>
                    l.IsPrimary &&
                    l.Coordinate != null &&
                    l.Coordinate.Latitude >= b.SouthLat &&
                    l.Coordinate.Latitude <= b.NorthLat &&
                    l.Coordinate.Longitude >= b.WestLon &&
                    l.Coordinate.Longitude <= b.EastLon);

            if (!string.IsNullOrWhiteSpace(request.CountryCode))
            {
                var cc = request.CountryCode.Trim();
                primaryLocationQuery = primaryLocationQuery.Where(l =>
                    l.CountryCode != null && l.CountryCode == cc);
            }

            var composed =
                from biz in businessQuery
                join loc in primaryLocationQuery
                    on biz.Id equals loc.BusinessId
                select new BusinessDiscoveryListItemDto
                {
                    Id = biz.Id,
                    Name = biz.Name,
                    ShortDescription = biz.ShortDescription,
                    Category = biz.Category,
                    IsActive = biz.IsActive,
                    City = loc.City,
                    Coordinate = new GeoCoordinateDto
                    {
                        Latitude = loc.Coordinate!.Latitude,
                        Longitude = loc.Coordinate.Longitude,
                        AltitudeMeters = loc.Coordinate.AltitudeMeters
                    },
                    PrimaryImageUrl = _db.Set<BusinessMedia>()
                        .AsNoTracking()
                        .Where(m => m.BusinessId == biz.Id && m.IsPrimary)
                        .OrderBy(m => m.SortOrder)
                        .Select(m => m.Url)
                        .FirstOrDefault()
                };

            var total = await composed.CountAsync(ct).ConfigureAwait(false);

            var items = await composed
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns a paged list of business locations for Admin grids.
    /// </summary>
    public sealed class GetBusinessLocationsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        public GetBusinessLocationsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<BusinessLocationListItemDto> Items, int Total)> HandleAsync(
            Guid businessId, int page, int pageSize, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            baseQuery = filter switch
            {
                BusinessLocationQueueFilter.Primary => baseQuery.Where(x => x.IsPrimary),
                BusinessLocationQueueFilter.MissingAddress => baseQuery.Where(x =>
                    x.AddressLine1 == null ||
                    x.AddressLine1.Trim() == string.Empty ||
                    x.City == null ||
                    x.City.Trim() == string.Empty ||
                    x.CountryCode == null ||
                    x.CountryCode.Trim() == string.Empty),
                BusinessLocationQueueFilter.MissingCoordinates => baseQuery.Where(x => x.Coordinate == null),
                _ => baseQuery
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.Name, q, QueryLikePattern.EscapeCharacter) ||
                    (x.City != null && EF.Functions.Like(x.City, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.PostalCode != null && EF.Functions.Like(x.PostalCode, q, QueryLikePattern.EscapeCharacter)));
            }

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BusinessLocationListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    City = x.City,
                    Region = x.Region,
                    CountryCode = x.CountryCode,
                    IsPrimary = x.IsPrimary,
                    HasAddress =
                        x.AddressLine1 != null &&
                        x.AddressLine1.Trim() != string.Empty &&
                        x.City != null &&
                        x.City.Trim() != string.Empty &&
                        x.CountryCode != null &&
                        x.CountryCode.Trim() != string.Empty,
                    HasCoordinates = x.Coordinate != null,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<BusinessLocationOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var baseQuery = _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            var summary = await baseQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PrimaryCount = g.Count(x => x.IsPrimary),
                    MissingAddressCount = g.Count(x =>
                        x.AddressLine1 == null ||
                        x.AddressLine1.Trim() == string.Empty ||
                        x.City == null ||
                        x.City.Trim() == string.Empty ||
                        x.CountryCode == null ||
                        x.CountryCode.Trim() == string.Empty),
                    MissingCoordinatesCount = g.Count(x => x.Coordinate == null)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return new BusinessLocationOpsSummaryDto
            {
                TotalCount = summary?.TotalCount ?? 0,
                PrimaryCount = summary?.PrimaryCount ?? 0,
                MissingAddressCount = summary?.MissingAddressCount ?? 0,
                MissingCoordinatesCount = summary?.MissingCoordinatesCount ?? 0
            };
        }
    }
}

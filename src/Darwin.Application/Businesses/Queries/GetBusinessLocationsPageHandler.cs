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
                    string.IsNullOrWhiteSpace(x.AddressLine1) ||
                    string.IsNullOrWhiteSpace(x.City) ||
                    string.IsNullOrWhiteSpace(x.CountryCode)),
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
                        !string.IsNullOrWhiteSpace(x.AddressLine1) &&
                        !string.IsNullOrWhiteSpace(x.City) &&
                        !string.IsNullOrWhiteSpace(x.CountryCode),
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

            return new BusinessLocationOpsSummaryDto
            {
                TotalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false),
                PrimaryCount = await baseQuery.CountAsync(x => x.IsPrimary, ct).ConfigureAwait(false),
                MissingAddressCount = await baseQuery.CountAsync(
                    x => string.IsNullOrWhiteSpace(x.AddressLine1) ||
                         string.IsNullOrWhiteSpace(x.City) ||
                         string.IsNullOrWhiteSpace(x.CountryCode),
                    ct).ConfigureAwait(false),
                MissingCoordinatesCount = await baseQuery.CountAsync(x => x.Coordinate == null, ct).ConfigureAwait(false)
            };
        }
    }
}

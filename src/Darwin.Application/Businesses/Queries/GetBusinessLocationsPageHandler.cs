using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns a paged list of business locations for Admin grids.
    /// </summary>
    public sealed class GetBusinessLocationsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetBusinessLocationsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<BusinessLocationListItemDto> Items, int Total)> HandleAsync(
            Guid businessId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId);

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
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

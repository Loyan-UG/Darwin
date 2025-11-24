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
    /// Returns a paged list of businesses for Admin grids.
    /// Pure query with AsNoTracking; respects global soft-delete filter.
    /// </summary>
    public sealed class GetBusinessesPageHandler
    {
        private readonly IAppDbContext _db;
        public GetBusinessesPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<BusinessListItemDto> Items, int Total)> HandleAsync(
            int page, int pageSize, string? query = null, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Business>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.Name.Contains(q) ||
                    (x.LegalName != null && x.LegalName.Contains(q)));
            }

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(x => x.ModifiedAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BusinessListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    Category = x.Category,
                    IsActive = x.IsActive,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

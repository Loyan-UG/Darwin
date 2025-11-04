using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Returns a paged list of add-on groups for Admin.
    /// </summary>
    public sealed class GetAddOnGroupsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetAddOnGroupsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<AddOnGroupListItemDto> Items, int Total)> 
            HandleAsync(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var baseQuery = _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => !g.IsDeleted &&
                            (string.IsNullOrEmpty(q) || g.Name.Contains(q)))
                .Select(g => new AddOnGroupListItemDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Currency = g.Currency,
                    IsActive = g.IsActive,
                    IsGlobal = g.IsGlobal,
                    OptionsCount = g.Options.Count,
                    ModifiedAtUtc = g.ModifiedAtUtc,
                    RowVersion = g.RowVersion
                });

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(x => x.ModifiedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

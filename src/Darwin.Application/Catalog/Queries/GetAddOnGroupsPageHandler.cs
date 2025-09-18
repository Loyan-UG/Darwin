using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Returns a paged list of add-on groups for Admin.
    /// </summary>
    public sealed class GetAddOnGroupsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetAddOnGroupsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<AddOnGroupListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<AddOnGroup>().AsNoTracking().Where(g => !g.IsDeleted);

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(g => g.ModifiedAtUtc ?? g.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(g => new AddOnGroupListItemDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Currency = g.Currency,
                    IsGlobal = g.IsGlobal,
                    IsActive = g.IsActive,
                    OptionsCount = g.Options.Count,
                    ModifiedAtUtc = g.ModifiedAtUtc
                }).ToListAsync(ct);

            return (items, total);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Queries
{
    /// <summary>
    /// Paged list of menus for Admin grid.
    /// </summary>
    public sealed class GetMenusPageHandler
    {
        private readonly IAppDbContext _db;
        public GetMenusPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<MenuListItemDto> Items, int Total)> HandleAsync(
            int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Menu>().AsNoTracking();
            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MenuListItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    ItemsCount = m.Items.Count,
                    ModifiedAtUtc = m.ModifiedAtUtc
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

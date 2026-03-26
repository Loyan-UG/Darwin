using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Returns a paged list of categories with a culture-specific display name.
    /// </summary>
    public sealed class GetCategoriesPageHandler
    {
        private readonly IAppDbContext _db;
        public GetCategoriesPageHandler(IAppDbContext db) => _db = db;

        public async Task<(IReadOnlyList<CategoryListItemDto> Items, int Total)>
            HandleAsync(int page = 1, int pageSize = 20, string? culture = "de-DE", CancellationToken ct = default)
        {
            return await HandleAsync(page, pageSize, culture, query: null, ct);
        }

        public async Task<(IReadOnlyList<CategoryListItemDto> Items, int Total)>
            HandleAsync(int page, int pageSize, string? culture, string? query, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

            var q = _db.Set<Category>()
                .AsNoTracking()
                .Where(c =>
                    query == null ||
                    c.Translations.Any(t => t.Name.Contains(query)) ||
                    c.Translations.Any(t => t.Slug != null && t.Slug.Contains(query)));

            var total = await q.CountAsync(ct);
            var items = await q
                .Select(c => new CategoryListItemDto
                {
                    Id = c.Id,
                    Name = c.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                        ?? c.Translations.Select(t => t.Name).FirstOrDefault(),
                    IsActive = c.IsActive,
                    SortOrder = c.SortOrder,
                    RowVersion = c.RowVersion,
                    ParentId = c.ParentId
                })
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    ///     Paged query handler for listing products in Admin, returning lightweight rows suitable
    ///     for grid rendering (name, brand, primary category, status, modified date, etc.).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Performance:
    ///         <list type="bullet">
    ///             <item>Projects directly with <c>Select</c> into DTOs to avoid materializing full aggregates.</item>
    ///             <item>Uses <c>AsNoTracking</c> to reduce overhead for read-only scenarios.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Localization:
    ///         Selects the best translation for the provided culture with a sensible fallback.
    ///     </para>
    /// </remarks>
    public sealed class GetProductsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetProductsPageHandler(IAppDbContext db) { _db = db; }

        public async Task<(IReadOnlyList<ProductListItemDto> Items, int Total)> HandleAsync(int page = 1, int pageSize = 20, string? culture = "de-DE", CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var query = _db.Set<Product>()
                .AsNoTracking()
                .Select(p => new ProductListItemDto
                {
                    Id = p.Id,
                    DefaultName = p.Translations
                        .Where(t => t.Culture == culture)
                        .Select(t => t.Name)
                        .FirstOrDefault() ?? p.Translations.Select(t => t.Name).FirstOrDefault(),
                    IsActive = p.IsActive,
                    IsVisible = p.IsVisible,
                    VariantCount = p.Variants.Count
                });

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(x => x.DefaultName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

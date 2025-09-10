using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    ///     Provides lightweight lookup data (Brands, TaxCategories, Categories) for Admin forms
    ///     to populate drop-downs without loading full aggregates.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Performance:
    ///         <list type="bullet">
    ///             <item>Uses <c>AsNoTracking</c> and <c>Select</c> into slim DTOs to minimize memory and avoid change tracking.</item>
    ///             <item>Orders by friendly fields (e.g., Name, SortOrder) for predictable UX.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Localization:
    ///         Category names are resolved per-culture; if a translation is missing, a best-effort fallback is returned.
    ///     </para>
    ///     <para>
    ///         Intended to be called by Admin controllers; cache results where appropriate to reduce DB load.
    ///     </para>
    /// </remarks>
    public sealed class GetCatalogLookupsHandler
    {
        private readonly IAppDbContext _db;
        public GetCatalogLookupsHandler(IAppDbContext db) => _db = db;

        public async Task<CatalogLookupsDto> HandleAsync(string culture = "de-DE", CancellationToken ct = default)
        {
            var brands = await _db.Set<Darwin.Domain.Entities.Catalog.Brand>()
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new LookupItem { Id = b.Id, Name = b.Name })
                .ToListAsync(ct);

            var tax = await _db.Set<Darwin.Domain.Entities.Pricing.TaxCategory>()
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new LookupItem { Id = t.Id, Name = $"{t.Name} ({t.VatRate:P0})" })
                .ToListAsync(ct);

            var categories = await _db.Set<Darwin.Domain.Entities.Catalog.Category>()
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .Select(c => new LookupItem
                {
                    Id = c.Id,
                    Name = c.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                        ?? c.Translations.Select(t => t.Name).FirstOrDefault() ?? "?"
                })
                .ToListAsync(ct);

            return new CatalogLookupsDto
            {
                Brands = brands,
                TaxCategories = tax,
                Categories = categories
            };
        }
    }

    public sealed class CatalogLookupsDto
    {
        public List<LookupItem> Brands { get; set; } = new();
        public List<LookupItem> TaxCategories { get; set; } = new();
        public List<LookupItem> Categories { get; set; } = new();
    }

    public sealed class LookupItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

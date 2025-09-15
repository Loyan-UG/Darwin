using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    ///     Provides light-weight lookup data for Admin drop-downs: Brands, TaxCategories, and Categories.
    ///     Brand names and Category names are resolved per-culture (with fallback) to support multilingual UIs.
    /// </summary>
    public sealed class GetCatalogLookupsHandler
    {
        private readonly IAppDbContext _db;
        public GetCatalogLookupsHandler(IAppDbContext db) => _db = db;

        public async Task<CatalogLookupsDto> HandleAsync(string culture = "de-DE", CancellationToken ct = default)
        {
            // Brands: resolve per-culture name; fallback to any available translation or "?"
            var brands = await _db.Set<Brand>()
                .AsNoTracking()
                .Select(b => new LookupItem
                {
                    Id = b.Id,
                    Name = b.Translations
                        .Where(t => t.Culture == culture)
                        .Select(t => t.Name)
                        .FirstOrDefault()
                        ?? b.Translations.Select(t => t.Name).FirstOrDefault()
                        ?? "?"
                })
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            // Tax categories: as before
            var tax = await _db.Set<Darwin.Domain.Entities.Pricing.TaxCategory>()
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new LookupItem { Id = t.Id, Name = $"{t.Name} ({t.VatRate:P0})" })
                .ToListAsync(ct);

            // Categories: per-culture (existing)
            var categories = await _db.Set<Darwin.Domain.Entities.Catalog.Category>()
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .Select(c => new LookupItem
                {
                    Id = c.Id,
                    Name = c.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                        ?? c.Translations.Select(t => t.Name).FirstOrDefault()
                        ?? "?"
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

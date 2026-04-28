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
    ///     Paged query handler for listing products in Admin, returning lightweight rows suitable
    ///     for grid rendering (name, brand, primary category, status, modified date, etc.).
    /// </summary>
    public sealed class GetProductsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetProductsPageHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<(IReadOnlyList<ProductListItemDto> Items, int Total)> HandleAsync(
            int page = 1,
            int pageSize = 20,
            string? culture = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCultureDefault,
            CancellationToken ct = default)
        {
            return await HandleAsync(page, pageSize, culture, query: null, filter: null, ct);
        }

        public async Task<(IReadOnlyList<ProductListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? culture,
            string? query,
            string? filter,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
            filter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim().ToLowerInvariant();

            var productsQuery = _db.Set<Product>()
                .AsNoTracking()
                .Where(p =>
                    !p.IsDeleted &&
                    (query == null ||
                     p.Translations.Any(t => !t.IsDeleted && t.Name.Contains(query)) ||
                     p.Translations.Any(t => !t.IsDeleted && t.Slug != null && t.Slug.Contains(query)) ||
                     p.Variants.Any(v => !v.IsDeleted && v.Sku.Contains(query))));

            productsQuery = filter switch
            {
                "inactive" => productsQuery.Where(p => !p.IsActive),
                "hidden" => productsQuery.Where(p => !p.IsVisible),
                "single-variant" => productsQuery.Where(p => p.Variants.Count(v => !v.IsDeleted) == 1),
                "scheduled" => productsQuery.Where(p => p.PublishStartUtc != null || p.PublishEndUtc != null),
                _ => productsQuery
            };

            var projectedQuery = productsQuery.Select(p => new ProductListItemDto
            {
                Id = p.Id,
                DefaultName = p.Translations
                    .Where(t => !t.IsDeleted && t.Culture == culture)
                    .Select(t => t.Name)
                    .FirstOrDefault() ?? p.Translations.Where(t => !t.IsDeleted).Select(t => t.Name).FirstOrDefault(),
                IsActive = p.IsActive,
                IsVisible = p.IsVisible,
                VariantCount = p.Variants.Count(v => !v.IsDeleted),
                PublishStartUtc = p.PublishStartUtc,
                PublishEndUtc = p.PublishEndUtc,
                ModifiedAtUtc = p.ModifiedAtUtc ?? p.CreatedAtUtc,
                RowVersion = p.RowVersion
            });

            var total = await projectedQuery.CountAsync(ct);
            var items = await projectedQuery
                .OrderBy(x => x.DefaultName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetProductOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetProductOpsSummaryHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<ProductOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var products = _db.Set<Product>().AsNoTracking().Where(p => !p.IsDeleted);

            return new ProductOpsSummaryDto
            {
                TotalCount = await products.CountAsync(ct),
                InactiveCount = await products.CountAsync(p => !p.IsActive, ct),
                HiddenCount = await products.CountAsync(p => !p.IsVisible, ct),
                SingleVariantCount = await products.CountAsync(p => p.Variants.Count(v => !v.IsDeleted) == 1, ct),
                ScheduledCount = await products.CountAsync(p => p.PublishStartUtc != null || p.PublishEndUtc != null, ct)
            };
        }
    }
}

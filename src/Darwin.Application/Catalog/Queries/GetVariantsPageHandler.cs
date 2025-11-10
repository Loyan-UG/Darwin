using System;
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
    ///     Returns a paged list of product variants for selection in admin UIs.
    ///     Resolves the owning product's display name using translations for a given culture,
    ///     with fallback to any available translation.
    /// </summary>
    public sealed class GetVariantsPageHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        ///     Creates a new instance of <see cref="GetVariantsPageHandler"/>.
        /// </summary>
        public GetVariantsPageHandler(IAppDbContext db) => _db = db;

        /// <summary>
        ///     Executes the paged query.
        /// </summary>
        /// <param name="page">1-based page index.</param>
        /// <param name="pageSize">Items per page (clamped to a sensible range).</param>
        /// <param name="query">Optional search term (matches SKU or product name).</param>
        /// <param name="culture">IETF culture code (e.g., "de-DE") used for translation lookup.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A tuple of the requested page items and the total count.</returns>
        public async Task<(IReadOnlyList<ProductVariantListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query,
            string culture,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            pageSize = Math.Clamp(pageSize, 1, 200);

            var variants = _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => !v.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                variants = variants.Where(v =>
                    EF.Functions.Like(v.Sku, $"%{q}%") ||
                    _db.Set<ProductTranslation>()
                       .Any(t => t.ProductId == v.ProductId && !t.IsDeleted && EF.Functions.Like(t.Name, $"%{q}%")));
            }

            var total = await variants.CountAsync(ct);

            // NOTE: Avoids navigation on Variant -> Product; resolves ProductName via ProductTranslation.
            var items = await variants
                .OrderBy(v => v.Sku) // Optionally add a secondary order by product name.
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new ProductVariantListItemDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    Sku = v.Sku,
                    Currency = v.Currency,
                    BasePriceNetMinor = v.BasePriceNetMinor,
                    StockOnHand = v.StockOnHand,
                    IsDigital = v.IsDigital,
                    ProductName =
                        _db.Set<ProductTranslation>()
                           .Where(t => t.ProductId == v.ProductId && !t.IsDeleted && t.Culture == culture)
                           .Select(t => t.Name)
                           .FirstOrDefault()
                        ?? _db.Set<ProductTranslation>()
                              .Where(t => t.ProductId == v.ProductId && !t.IsDeleted)
                              .Select(t => t.Name)
                              .FirstOrDefault(),
                    RowVersion = v.RowVersion,
                    Gtin = v.Gtin
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries.GetProductForEdit
{
    public sealed class GetProductForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetProductForEditHandler(IAppDbContext db) { _db = db; }

        public async Task<ProductEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var product = await _db.Set<Product>()
                .Include(p => p.Translations)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product == null) return null;

            return new ProductEditDto
            {
                Id = product.Id,
                BrandId = product.BrandId,
                PrimaryCategoryId = product.PrimaryCategoryId,
                Kind = product.Kind.ToString(),
                RowVersion = product.RowVersion,
                Translations = product.Translations.Select(t => new ProductTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    ShortDescription = t.ShortDescription,
                    FullDescriptionHtml = t.FullDescriptionHtml,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    SearchKeywords = t.SearchKeywords
                }).ToList(),
                Variants = product.Variants.Select(v => new ProductVariantCreateDto
                {
                    Sku = v.Sku,
                    Gtin = v.Gtin,
                    ManufacturerPartNumber = v.ManufacturerPartNumber,
                    BasePriceNetMinor = v.BasePriceNetMinor,
                    CompareAtPriceNetMinor = v.CompareAtPriceNetMinor,
                    Currency = v.Currency,
                    TaxCategoryId = v.TaxCategoryId,
                    StockOnHand = v.StockOnHand,
                    StockReserved = v.StockReserved,
                    ReorderPoint = v.ReorderPoint,
                    BackorderAllowed = v.BackorderAllowed,
                    MinOrderQty = v.MinOrderQty,
                    MaxOrderQty = v.MaxOrderQty,
                    StepOrderQty = v.StepOrderQty,
                    PackageWeight = v.PackageWeight,
                    PackageLength = v.PackageLength,
                    PackageWidth = v.PackageWidth,
                    PackageHeight = v.PackageHeight,
                    IsDigital = v.IsDigital
                }).ToList()
            };
        }
    }
}

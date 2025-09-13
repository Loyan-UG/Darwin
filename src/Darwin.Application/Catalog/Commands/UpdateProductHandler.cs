using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Darwin.Application.Common.Html;


namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    ///     Handler that updates an existing product, honoring optimistic concurrency,
    ///     synchronizing translations and variants, and persisting changes atomically.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Concurrency:
    ///         <list type="bullet">
    ///             <item>Compare RowVersion to detect conflicts; on mismatch, throw and let the controller report.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Mapping:
    ///         Merge strategy should avoid destructive overwrites (e.g., preserve existing variant identities where possible).
    ///     </para>
    /// </remarks>
    public sealed class UpdateProductHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ProductEditDto> _validator;
        private readonly IHtmlSanitizer _sanitizer;

        public UpdateProductHandler(IAppDbContext db, IValidator<ProductEditDto> validator, IHtmlSanitizer sanitizer)
        {
            _db = db;
            _validator = validator;
            _sanitizer = sanitizer;
        }   

        public async Task HandleAsync(ProductEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var product = await _db.Set<Product>()
                .Include(p => p.Translations)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == dto.Id, ct)
                ?? throw new ValidationException("Product not found.");


            // Concurrency check
            if (!product.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("The product was modified by another user. Please reload and try again.");

            // Map basic fields
            product.BrandId = dto.BrandId;
            product.PrimaryCategoryId = dto.PrimaryCategoryId;
            product.Kind = ParseKind(dto.Kind);

            // Replace translations (simplest approach for now)
            product.Translations.Clear();
            foreach (var t in dto.Translations)
            {
                product.Translations.Add(new ProductTranslation
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    ShortDescription = t.ShortDescription,
                    FullDescriptionHtml = HtmlSanitizerHelper.SanitizeOrNull(_sanitizer, t.FullDescriptionHtml),
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    SearchKeywords = t.SearchKeywords
                });
            }

            // Replace variants (simplest approach for now)
            product.Variants.Clear();
            foreach (var v in dto.Variants)
            {
                product.Variants.Add(new ProductVariant
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
                });
            }

            await _db.SaveChangesAsync(ct);
        }

        private static ProductKind ParseKind(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "simple" => ProductKind.Simple,
                "variant" => ProductKind.Variant,
                "bundle" => ProductKind.Bundle,
                "digital" => ProductKind.Digital,
                "service" => ProductKind.Service,
                _ => ProductKind.Simple
            };
        }
    }
}

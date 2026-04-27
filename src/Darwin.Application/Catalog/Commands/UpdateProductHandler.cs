using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Localization;


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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateProductHandler(
            IAppDbContext db,
            IValidator<ProductEditDto> validator,
            IHtmlSanitizer sanitizer,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _sanitizer = sanitizer;
            _localizer = localizer;
        }   

        public async Task HandleAsync(ProductEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var product = await _db.Set<Product>()
                .Include(p => p.Translations)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == dto.Id, ct)
                ?? throw new ValidationException(_localizer["ProductNotFound"]);


            // Concurrency check
            if (!product.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ProductModifiedByAnotherUserPleaseReload"]);

            // Map basic fields
            product.BrandId = dto.BrandId;
            product.PrimaryCategoryId = dto.PrimaryCategoryId;
            product.Kind = ParseKind(dto.Kind);

            SyncTranslations(product, dto);
            SyncVariants(product, dto);

            await _db.SaveChangesAsync(ct);
        }

        private void SyncTranslations(Product product, ProductEditDto dto)
        {
            var requestedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var translationsByCulture = product.Translations
                .Where(t => !t.IsDeleted)
                .ToDictionary(t => t.Culture, StringComparer.OrdinalIgnoreCase);

            foreach (var input in dto.Translations)
            {
                var culture = input.Culture.Trim();
                if (!requestedCultures.Add(culture))
                {
                    throw new ValidationException(_localizer["DuplicateVariantLinesNotAllowed"]);
                }

                if (!translationsByCulture.TryGetValue(culture, out var translation))
                {
                    translation = new ProductTranslation
                    {
                        ProductId = product.Id,
                        Culture = culture
                    };
                    product.Translations.Add(translation);
                }

                translation.Name = input.Name.Trim();
                translation.Slug = input.Slug.Trim();
                translation.ShortDescription = input.ShortDescription?.Trim();
                translation.FullDescriptionHtml = HtmlSanitizerHelper.SanitizeOrNull(_sanitizer, input.FullDescriptionHtml);
                translation.MetaTitle = input.MetaTitle?.Trim();
                translation.MetaDescription = input.MetaDescription?.Trim();
                translation.SearchKeywords = input.SearchKeywords?.Trim();
                translation.IsDeleted = false;
            }

            foreach (var existing in product.Translations.Where(t => !t.IsDeleted).ToList())
            {
                if (!requestedCultures.Contains(existing.Culture))
                {
                    existing.IsDeleted = true;
                }
            }
        }

        private void SyncVariants(Product product, ProductEditDto dto)
        {
            var existingById = product.Variants
                .Where(v => !v.IsDeleted)
                .ToDictionary(v => v.Id);
            var existingBySku = product.Variants
                .Where(v => !v.IsDeleted)
                .GroupBy(v => v.Sku, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var retainedVariantIds = new HashSet<Guid>();
            var requestedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in dto.Variants)
            {
                var sku = input.Sku.Trim();
                if (!requestedSkus.Add(sku))
                {
                    throw new ValidationException(_localizer["DuplicateVariantLinesNotAllowed"]);
                }

                ProductVariant variant;
                if (input.Id.HasValue && existingById.TryGetValue(input.Id.Value, out var existingByRequestedId))
                {
                    variant = existingByRequestedId;
                }
                else if (existingBySku.TryGetValue(sku, out var existingByRequestedSku))
                {
                    variant = existingByRequestedSku;
                }
                else
                {
                    variant = new ProductVariant
                    {
                        ProductId = product.Id
                    };
                    product.Variants.Add(variant);
                }

                ApplyVariant(input, variant);
                variant.IsDeleted = false;
                retainedVariantIds.Add(variant.Id);
            }

            foreach (var existing in product.Variants.Where(v => !v.IsDeleted).ToList())
            {
                if (!retainedVariantIds.Contains(existing.Id))
                {
                    existing.IsDeleted = true;
                }
            }
        }

        private static void ApplyVariant(ProductVariantCreateDto input, ProductVariant variant)
        {
            variant.Sku = input.Sku.Trim();
            variant.Gtin = input.Gtin?.Trim();
            variant.ManufacturerPartNumber = input.ManufacturerPartNumber?.Trim();
            variant.BasePriceNetMinor = input.BasePriceNetMinor;
            variant.CompareAtPriceNetMinor = input.CompareAtPriceNetMinor;
            variant.Currency = input.Currency.Trim().ToUpperInvariant();
            variant.TaxCategoryId = input.TaxCategoryId;
            variant.StockOnHand = input.StockOnHand;
            variant.StockReserved = input.StockReserved;
            variant.ReorderPoint = input.ReorderPoint;
            variant.BackorderAllowed = input.BackorderAllowed;
            variant.MinOrderQty = input.MinOrderQty;
            variant.MaxOrderQty = input.MaxOrderQty;
            variant.StepOrderQty = input.StepOrderQty;
            variant.PackageWeight = input.PackageWeight;
            variant.PackageLength = input.PackageLength;
            variant.PackageWidth = input.PackageWidth;
            variant.PackageHeight = input.PackageHeight;
            variant.IsDigital = input.IsDigital;
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

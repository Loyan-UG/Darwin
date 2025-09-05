using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Catalog
{
    public sealed class ProductTranslationVm
    {
        public string Culture { get; set; } = "de-DE";
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? FullDescriptionHtml { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? SearchKeywords { get; set; }
    }

    public sealed class ProductVariantCreateVm
    {
        public string Sku { get; set; } = string.Empty;
        public string? Gtin { get; set; }
        public string? ManufacturerPartNumber { get; set; }
        public long BasePriceNetMinor { get; set; }
        public long? CompareAtPriceNetMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public Guid TaxCategoryId { get; set; }
        public int StockOnHand { get; set; }
        public int StockReserved { get; set; }
        public int? ReorderPoint { get; set; }
        public bool BackorderAllowed { get; set; }
        public int? MinOrderQty { get; set; }
        public int? MaxOrderQty { get; set; }
        public int? StepOrderQty { get; set; }
        public int? PackageWeight { get; set; }
        public int? PackageLength { get; set; }
        public int? PackageWidth { get; set; }
        public int? PackageHeight { get; set; }
        public bool IsDigital { get; set; }
    }

    public sealed class ProductCreateVm
    {
        public Guid? BrandId { get; set; }
        public Guid? PrimaryCategoryId { get; set; }
        public string Kind { get; set; } = "Simple";

        // Ensure these collections are always non-null and have one default row for initial rendering.
        public List<ProductTranslationVm> Translations { get; set; }
        public List<ProductVariantCreateVm> Variants { get; set; }

        public ProductCreateVm()
        {
            Translations = new List<ProductTranslationVm> { new() { Culture = "de-DE" } };
            Variants = new List<ProductVariantCreateVm> { new() { Currency = "EUR" } };
        }
    }
}

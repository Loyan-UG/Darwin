using System;
using System.Collections.Generic;

namespace Darwin.Application.Catalog.DTOs
{
    /// <summary>Translation slice for product.</summary>
    public sealed class ProductTranslationDto
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

    /// <summary>Create DTO for product variant.</summary>
    public sealed class ProductVariantCreateDto
    {
        public string Sku { get; set; } = string.Empty;
        public string? Gtin { get; set; }
        public string? ManufacturerPartNumber { get; set; }

        // Pricing (NET) in minor units
        public long BasePriceNetMinor { get; set; }
        public long? CompareAtPriceNetMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public Guid TaxCategoryId { get; set; }

        // Inventory
        public int StockOnHand { get; set; }
        public int StockReserved { get; set; }
        public int? ReorderPoint { get; set; }
        public bool BackorderAllowed { get; set; }
        public int? MinOrderQty { get; set; }
        public int? MaxOrderQty { get; set; }
        public int? StepOrderQty { get; set; }

        // Logistics (SI base units)
        public int? PackageWeight { get; set; }
        public int? PackageLength { get; set; }
        public int? PackageWidth { get; set; }
        public int? PackageHeight { get; set; }
        public bool IsDigital { get; set; }
    }

    /// <summary>Create DTO for product aggregate.</summary>
    public sealed class ProductCreateDto
    {
        public Guid? BrandId { get; set; }
        public Guid? PrimaryCategoryId { get; set; }
        public string Kind { get; set; } = "Simple"; // validates to enum later

        public List<ProductTranslationDto> Translations { get; set; } = new();
        public List<ProductVariantCreateDto> Variants { get; set; } = new(); // at least one
    }
}

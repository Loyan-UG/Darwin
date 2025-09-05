using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;


namespace Darwin.Domain.Entities.Catalog
{
    /// <summary>
    /// Product aggregate root with translation, media, variants, and SEO.
    /// </summary>
    public sealed class Product : BaseEntity
    {
        /// <summary>Manufacturer or brand reference (optional).</summary>
        public Guid? BrandId { get; set; }
        /// <summary>Primary (default) category for breadcrumbs; product can belong to multiple categories via join table.</summary>
        public Guid? PrimaryCategoryId { get; set; }
        /// <summary>Visibility and lifecycle control flags.</summary>
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        /// <summary>Optional scheduled publishing window in UTC.</summary>
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        /// <summary>Product kind. Phase 1 uses Simple/Variant; others reserved for future.</summary>
        public ProductKind Kind { get; set; } = ProductKind.Simple;
        /// <summary>Collections of translations for culture-specific fields such as Name and Slug.</summary>
        public List<ProductTranslation> Translations { get; set; } = new();
        /// <summary>Media gallery association items (M:N with sort/role).</summary>
        public List<ProductMedia> Media { get; set; } = new();
        /// <summary>Variants associated with this product (at least one). For Simple products, a single default variant is used.</summary>
        public List<ProductVariant> Variants { get; set; } = new();
        /// <summary>Configurable options (e.g., Color, Size) that generate variant combinations.</summary>
        public List<ProductOption> Options { get; set; } = new();
        /// <summary>Related product ids for cross-navigation (simple relation; advanced cross/upsell later).</summary>
        public List<Guid> RelatedProductIds { get; set; } = new();
    }

    /// <summary>
    /// Per-culture translation for product including SEO and descriptions.
    /// </summary>
    public sealed class ProductTranslation : CMS.TranslationBase
    {
        public Guid ProductId { get; set; }
        /// <summary>Localized product display name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Unique slug per culture for routing and SEO.</summary>
        public string Slug { get; set; } = string.Empty;
        /// <summary>Optional short description for list cards.</summary>
        public string? ShortDescription { get; set; }
        /// <summary>Sanitized HTML for detailed description.</summary>
        public string? FullDescriptionHtml { get; set; }
        /// <summary>Optional custom SEO meta title.</summary>
        public string? MetaTitle { get; set; }
        /// <summary>Optional SEO meta description.</summary>
        public string? MetaDescription { get; set; }
        /// <summary>Optional keywords to aid in-site search ranking (not for SEO engines).</summary>
        public string? SearchKeywords { get; set; }
    }


    /// <summary>
    /// Join entity linking products to media assets with ordering and role.
    /// </summary>
    public sealed class ProductMedia : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Guid MediaAssetId { get; set; }
        /// <summary>Determines gallery order (lower first).</summary>
        public int SortOrder { get; set; }
        /// <summary>Role classification like Primary, Gallery, Thumbnail.</summary>
        public string? Role { get; set; }
    }

    /// <summary>
    /// Configurable option (e.g., Color) that can have multiple values (e.g., Red, Blue).
    /// Variants refer to specific option values via VariantOptionValue.
    /// </summary>
    public sealed class ProductOption : BaseEntity
    {
        public Guid ProductId { get; set; }
        /// <summary>Display name for the option (e.g., "Color").</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Ordering among options.</summary>
        public int SortOrder { get; set; }
        public List<ProductOptionValue> Values { get; set; } = new();
    }


    /// <summary>
    /// Concrete value for a product option (e.g., "Red").
    /// </summary>
    public sealed class ProductOptionValue : BaseEntity
    {
        public Guid ProductOptionId { get; set; }
        /// <summary>Human-readable value (e.g., "Red").</summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>Optional color hex or display hint.</summary>
        public string? ColorHex { get; set; }
        /// <summary>Ordering among values.</summary>
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Product variant captures sellable SKU with independent pricing, inventory, and logistics data.
    /// </summary>
    public sealed class ProductVariant : BaseEntity
    {
        /// <summary>Owning product id.</summary>
        public Guid ProductId { get; set; }
        /// <summary>Stock keeping unit; must be unique among active (non-deleted) variants.</summary>
        public string Sku { get; set; } = string.Empty;
        /// <summary>Optional barcode/GTIN/EAN for logistics integrations.</summary>
        public string? Gtin { get; set; }
        /// <summary>Manufacturer part number (optional).</summary>
        public string? ManufacturerPartNumber { get; set; }


        // Pricing (NET persistence in minor units)
        /// <summary>Base unit price stored in minor units (net, excluding VAT).</summary>
        public long BasePriceNetMinor { get; set; }
        /// <summary>Optional compare-at price (net) for strike-through pricing, stored in minor units.</summary>
        public long? CompareAtPriceNetMinor { get; set; }
        /// <summary>ISO 4217 currency code (phase 1: always "EUR").</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Tax category applied to this variant (maps to VAT rate e.g., 19% or 7%).</summary>
        public Guid TaxCategoryId { get; set; }


        // Inventory (single-warehouse in phase 1)
        /// <summary>Total physical stock on hand available.</summary>
        public int StockOnHand { get; set; }
        /// <summary>Quantity reserved for open orders (not yet shipped).</summary>
        public int StockReserved { get; set; }
        /// <summary>Minimum stock level triggering replenishment recommendations.</summary>
        public int? ReorderPoint { get; set; }
        /// <summary>Allow selling when stock is below zero.</summary>
        public bool BackorderAllowed { get; set; }
        /// <summary>Minimum quantity per order line.</summary>
        public int? MinOrderQty { get; set; }
        /// <summary>Maximum quantity per order line.</summary>
        public int? MaxOrderQty { get; set; }
        /// <summary>Step/increment for quantity selection (e.g., 5 → 5,10,15...).</summary>
        public int? StepOrderQty { get; set; }


        // Logistics — persist SI base units, *do not* encode units in property names
        /// <summary>Package weight stored in grams (display conversion per site settings).</summary>
        public int? PackageWeight { get; set; }
        /// <summary>Package length stored in millimeters (display conversion per site settings).</summary>
        public int? PackageLength { get; set; }
        /// <summary>Package width stored in millimeters (display conversion per site settings).</summary>
        public int? PackageWidth { get; set; }
        /// <summary>Package height stored in millimeters (display conversion per site settings).</summary>
        public int? PackageHeight { get; set; }
        /// <summary>Whether the variant is a digital good (no shipping required).</summary>
        public bool IsDigital { get; set; }


        /// <summary>Option values selected for this variant (Color=Red, Size=M).</summary>
        public System.Collections.Generic.List<VariantOptionValue> OptionValues { get; set; } = new();
    }


    /// <summary>
    /// Join entity pointing from a variant to a concrete product option value (e.g., Variant → (Color=Red)).
    /// </summary>
    public sealed class VariantOptionValue : Darwin.Domain.Common.BaseEntity
    {
        public Guid VariantId { get; set; }
        public Guid ProductOptionId { get; set; }
        public Guid ProductOptionValueId { get; set; }
    }
}
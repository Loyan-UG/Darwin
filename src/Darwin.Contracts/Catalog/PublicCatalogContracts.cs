using Darwin.Contracts.Common;

namespace Darwin.Contracts.Catalog;

/// <summary>
/// Public category summary used by storefront navigation and listing pages.
/// </summary>
public sealed class PublicCategorySummary
{
    /// <summary>Gets or sets the category identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optional parent category identifier.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the localized category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized category slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional localized description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the sort order among siblings.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Public product card used by storefront listing pages.
/// </summary>
public class PublicProductSummary
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the localized product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized product slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional short description.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Gets or sets the product currency.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the price in minor units.</summary>
    public long PriceMinor { get; set; }

    /// <summary>Gets or sets the optional compare-at price in minor units.</summary>
    public long? CompareAtPriceMinor { get; set; }

    /// <summary>Gets or sets the optional primary image URL.</summary>
    public string? PrimaryImageUrl { get; set; }
}

/// <summary>
/// Public product detail used by storefront product pages.
/// </summary>
public sealed class PublicProductDetail : PublicProductSummary
{
    /// <summary>Gets or sets the optional full HTML description.</summary>
    public string? FullDescriptionHtml { get; set; }

    /// <summary>Gets or sets the optional SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the optional SEO meta description.</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets the optional primary category identifier.</summary>
    public Guid? PrimaryCategoryId { get; set; }

    /// <summary>Gets or sets the variant snapshots.</summary>
    public IReadOnlyList<PublicProductVariant> Variants { get; set; } = Array.Empty<PublicProductVariant>();

    /// <summary>Gets or sets the media gallery items.</summary>
    public IReadOnlyList<PublicProductMedia> Media { get; set; } = Array.Empty<PublicProductMedia>();
}

/// <summary>
/// Public product variant used by storefront product pages.
/// </summary>
public sealed class PublicProductVariant
{
    /// <summary>Gets or sets the variant identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the SKU.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the base price in minor units.</summary>
    public long BasePriceNetMinor { get; set; }

    /// <summary>Gets or sets the optional compare-at price in minor units.</summary>
    public long? CompareAtPriceNetMinor { get; set; }

    /// <summary>Gets or sets whether backorders are allowed.</summary>
    public bool BackorderAllowed { get; set; }

    /// <summary>Gets or sets whether the variant is digital.</summary>
    public bool IsDigital { get; set; }
}

/// <summary>
/// Public media item used by storefront product pages.
/// </summary>
public sealed class PublicProductMedia
{
    /// <summary>Gets or sets the media identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the public media URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the alternate text.</summary>
    public string Alt { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the role classification.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets the gallery sort order.</summary>
    public int SortOrder { get; set; }
}

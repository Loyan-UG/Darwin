namespace Darwin.Application.Catalog.DTOs;

/// <summary>
/// Public category summary used by storefront navigation and listing pages.
/// </summary>
public sealed class PublicCategorySummaryDto
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

    /// <summary>Gets or sets the optional localized SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the optional localized SEO meta description.</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets the sort order among siblings.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Public product card projection used by storefront listing pages.
/// </summary>
public class PublicProductSummaryDto
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the localized product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized product slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional short description.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Gets or sets the currency of the default variant.</summary>
    public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;

    /// <summary>Gets or sets the lowest visible price in minor units.</summary>
    public long PriceMinor { get; set; }

    /// <summary>Gets or sets the optional compare-at price in minor units.</summary>
    public long? CompareAtPriceMinor { get; set; }

    /// <summary>Gets or sets the optional primary image URL.</summary>
    public string? PrimaryImageUrl { get; set; }
}

/// <summary>
/// Public product detail projection used by storefront detail pages.
/// </summary>
public sealed class PublicProductDetailDto : PublicProductSummaryDto
{
    /// <summary>Gets or sets the optional full HTML description.</summary>
    public string? FullDescriptionHtml { get; set; }

    /// <summary>Gets or sets the optional SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the optional SEO meta description.</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets the optional primary category id.</summary>
    public Guid? PrimaryCategoryId { get; set; }

    /// <summary>Gets or sets the public variant projections.</summary>
    public List<PublicProductVariantDto> Variants { get; set; } = new();

    /// <summary>Gets or sets the public media gallery projections.</summary>
    public List<PublicProductMediaDto> Media { get; set; } = new();
}

/// <summary>
/// Public variant projection used by storefront product detail pages.
/// </summary>
public sealed class PublicProductVariantDto
{
    /// <summary>Gets or sets the variant identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the sellable SKU.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency.</summary>
    public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;

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
/// Public media projection used by storefront product detail pages.
/// </summary>
public sealed class PublicProductMediaDto
{
    /// <summary>Gets or sets the media asset identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the public media URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the alternate text.</summary>
    public string Alt { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the role classification.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets the sort order.</summary>
    public int SortOrder { get; set; }
}

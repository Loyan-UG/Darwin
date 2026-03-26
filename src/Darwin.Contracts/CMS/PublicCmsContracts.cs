namespace Darwin.Contracts.Cms;

/// <summary>
/// Public CMS page summary used by storefront and SEO-aware listing screens.
/// </summary>
public class PublicPageSummary
{
    /// <summary>Gets or sets the page identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the localized page title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the optional meta description.</summary>
    public string? MetaDescription { get; set; }
}

/// <summary>
/// Public CMS page detail used by storefront content delivery.
/// </summary>
public sealed class PublicPageDetail : PublicPageSummary
{
    /// <summary>Gets or sets the localized sanitized HTML content.</summary>
    public string ContentHtml { get; set; } = string.Empty;
}

/// <summary>
/// Public menu used by storefront navigation.
/// </summary>
public sealed class PublicMenu
{
    /// <summary>Gets or sets the menu identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the internal menu name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the menu items.</summary>
    public IReadOnlyList<PublicMenuItem> Items { get; set; } = Array.Empty<PublicMenuItem>();
}

/// <summary>
/// Public menu item used by storefront navigation.
/// </summary>
public sealed class PublicMenuItem
{
    /// <summary>Gets or sets the menu item identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optional parent item identifier.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the localized label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the sort order among siblings.</summary>
    public int SortOrder { get; set; }
}

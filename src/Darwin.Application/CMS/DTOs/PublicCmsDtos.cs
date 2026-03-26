namespace Darwin.Application.CMS.DTOs;

/// <summary>
/// Lightweight published page summary used by public CMS listings.
/// </summary>
public class PublicPageSummaryDto
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
/// Detailed published page projection used by public CMS content delivery.
/// </summary>
public sealed class PublicPageDetailDto : PublicPageSummaryDto
{
    /// <summary>Gets or sets the localized sanitized HTML content.</summary>
    public string ContentHtml { get; set; } = string.Empty;
}

/// <summary>
/// Public menu projection used by storefront navigation.
/// </summary>
public sealed class PublicMenuDto
{
    /// <summary>Gets or sets the menu identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the internal menu name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized menu items.</summary>
    public List<PublicMenuItemDto> Items { get; set; } = new();
}

/// <summary>
/// Public menu item projection used by storefront navigation.
/// </summary>
public sealed class PublicMenuItemDto
{
    /// <summary>Gets or sets the menu item identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optional parent identifier.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the localized label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the sort order among siblings.</summary>
    public int SortOrder { get; set; }
}

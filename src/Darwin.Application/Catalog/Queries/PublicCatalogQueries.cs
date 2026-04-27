using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries;

/// <summary>
/// Returns published categories for public storefront delivery.
/// </summary>
public sealed class GetPublishedCategoriesHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublishedCategoriesHandler"/> class.
    /// </summary>
    public GetPublishedCategoriesHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>
    /// Returns a page of published categories for the requested culture.
    /// </summary>
    public async Task<(List<PublicCategorySummaryDto> Items, int Total)> HandleAsync(int page, int pageSize, string culture, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        culture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var defaultCulture = SiteSettingDto.DefaultCultureDefault;

        var baseQuery = _db.Set<Category>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsPublished);

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PublicCategorySummaryDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Name = x.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Name).FirstOrDefault()
                    ?? string.Empty,
                Slug = x.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Slug).FirstOrDefault()
                    ?? string.Empty,
                Description = x.Translations.Where(t => t.Culture == culture).Select(t => t.Description).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Description).FirstOrDefault(),
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}

/// <summary>
/// Returns published products for public storefront delivery.
/// </summary>
public sealed class GetPublishedProductsPageHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublishedProductsPageHandler"/> class.
    /// </summary>
    public GetPublishedProductsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>
    /// Returns a page of published products for the requested culture and optional category slug.
    /// </summary>
    public async Task<(List<PublicProductSummaryDto> Items, int Total)> HandleAsync(
        int page,
        int pageSize,
        string culture,
        string? categorySlug = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        culture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var defaultCulture = SiteSettingDto.DefaultCultureDefault;
        categorySlug = string.IsNullOrWhiteSpace(categorySlug) ? null : categorySlug.Trim();

        var nowUtc = DateTime.UtcNow;
        var baseQuery = _db.Set<Product>()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.IsVisible &&
                (!x.PublishStartUtc.HasValue || x.PublishStartUtc <= nowUtc) &&
                (!x.PublishEndUtc.HasValue || x.PublishEndUtc >= nowUtc));

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            baseQuery = baseQuery.Where(x =>
                x.PrimaryCategoryId.HasValue &&
                _db.Set<Category>().Any(category =>
                    category.Id == x.PrimaryCategoryId.Value &&
                    category.Translations.Any(translation =>
                        translation.Slug == categorySlug &&
                        (translation.Culture == culture || translation.Culture == defaultCulture))));
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PublicProductSummaryDto
            {
                Id = x.Id,
                Name = x.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Name).FirstOrDefault()
                    ?? string.Empty,
                Slug = x.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Slug).FirstOrDefault()
                    ?? string.Empty,
                ShortDescription = x.Translations.Where(t => t.Culture == culture).Select(t => t.ShortDescription).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.ShortDescription).FirstOrDefault(),
                Currency = x.Variants.OrderBy(v => v.Sku).Select(v => v.Currency).FirstOrDefault() ?? SiteSettingDto.DefaultCurrencyDefault,
                PriceMinor = x.Variants.OrderBy(v => v.BasePriceNetMinor).Select(v => v.BasePriceNetMinor).FirstOrDefault(),
                CompareAtPriceMinor = x.Variants.OrderBy(v => v.BasePriceNetMinor).Select(v => v.CompareAtPriceNetMinor).FirstOrDefault(),
                PrimaryImageUrl = (
                    from productMedia in x.Media
                    join mediaAsset in _db.Set<MediaAsset>() on productMedia.MediaAssetId equals mediaAsset.Id
                    orderby productMedia.SortOrder
                    select mediaAsset.Url
                ).FirstOrDefault()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}

/// <summary>
/// Returns a published product detail by localized slug for public storefront delivery.
/// </summary>
public sealed class GetPublishedProductBySlugHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublishedProductBySlugHandler"/> class.
    /// </summary>
    public GetPublishedProductBySlugHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>
    /// Returns a published product detail for the requested localized slug.
    /// </summary>
    public async Task<PublicProductDetailDto?> HandleAsync(string slug, string culture, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        culture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var defaultCulture = SiteSettingDto.DefaultCultureDefault;
        var normalizedSlug = slug.Trim();
        var nowUtc = DateTime.UtcNow;

        return await _db.Set<Product>()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.IsVisible &&
                (!x.PublishStartUtc.HasValue || x.PublishStartUtc <= nowUtc) &&
                (!x.PublishEndUtc.HasValue || x.PublishEndUtc >= nowUtc) &&
                x.Translations.Any(t => t.Slug == normalizedSlug && (t.Culture == culture || t.Culture == defaultCulture)))
            .Select(x => new PublicProductDetailDto
            {
                Id = x.Id,
                Name = x.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Name).FirstOrDefault()
                    ?? string.Empty,
                Slug = x.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Slug).FirstOrDefault()
                    ?? string.Empty,
                ShortDescription = x.Translations.Where(t => t.Culture == culture).Select(t => t.ShortDescription).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.ShortDescription).FirstOrDefault(),
                FullDescriptionHtml = x.Translations.Where(t => t.Culture == culture).Select(t => t.FullDescriptionHtml).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.FullDescriptionHtml).FirstOrDefault(),
                MetaTitle = x.Translations.Where(t => t.Culture == culture).Select(t => t.MetaTitle).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.MetaTitle).FirstOrDefault(),
                MetaDescription = x.Translations.Where(t => t.Culture == culture).Select(t => t.MetaDescription).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.MetaDescription).FirstOrDefault(),
                Currency = x.Variants.OrderBy(v => v.Sku).Select(v => v.Currency).FirstOrDefault() ?? SiteSettingDto.DefaultCurrencyDefault,
                PriceMinor = x.Variants.OrderBy(v => v.BasePriceNetMinor).Select(v => v.BasePriceNetMinor).FirstOrDefault(),
                CompareAtPriceMinor = x.Variants.OrderBy(v => v.BasePriceNetMinor).Select(v => v.CompareAtPriceNetMinor).FirstOrDefault(),
                PrimaryCategoryId = x.PrimaryCategoryId,
                PrimaryImageUrl = (
                    from productMedia in x.Media
                    join mediaAsset in _db.Set<MediaAsset>() on productMedia.MediaAssetId equals mediaAsset.Id
                    orderby productMedia.SortOrder
                    select mediaAsset.Url
                ).FirstOrDefault(),
                Variants = x.Variants
                    .OrderBy(v => v.Sku)
                    .Select(v => new PublicProductVariantDto
                    {
                        Id = v.Id,
                        Sku = v.Sku,
                        Currency = v.Currency,
                        BasePriceNetMinor = v.BasePriceNetMinor,
                        CompareAtPriceNetMinor = v.CompareAtPriceNetMinor,
                        BackorderAllowed = v.BackorderAllowed,
                        IsDigital = v.IsDigital
                    })
                    .ToList(),
                Media = (
                    from productMedia in x.Media
                    join mediaAsset in _db.Set<MediaAsset>() on productMedia.MediaAssetId equals mediaAsset.Id
                    orderby productMedia.SortOrder
                    select new PublicProductMediaDto
                    {
                        Id = mediaAsset.Id,
                        Url = mediaAsset.Url,
                        Alt = mediaAsset.Alt,
                        Title = mediaAsset.Title,
                        Role = productMedia.Role ?? mediaAsset.Role,
                        SortOrder = productMedia.SortOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

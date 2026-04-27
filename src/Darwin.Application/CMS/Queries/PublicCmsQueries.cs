using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Queries;

/// <summary>
/// Returns a page of published CMS pages for public delivery.
/// </summary>
public sealed class GetPublishedPagesPageHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublishedPagesPageHandler"/> class.
    /// </summary>
    public GetPublishedPagesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>
    /// Returns a page of published CMS pages for the requested culture.
    /// </summary>
    public async Task<(List<PublicPageSummaryDto> Items, int Total)> HandleAsync(int page, int pageSize, string culture, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        culture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var defaultCulture = SiteSettingDto.DefaultCultureDefault;

        var nowUtc = DateTime.UtcNow;
        var baseQuery = _db.Set<Page>()
            .AsNoTracking()
            .Where(x =>
                x.IsPublished &&
                x.Status == PageStatus.Published &&
                (!x.PublishStartUtc.HasValue || x.PublishStartUtc <= nowUtc) &&
                (!x.PublishEndUtc.HasValue || x.PublishEndUtc >= nowUtc));

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderBy(x => x.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PublicPageSummaryDto
            {
                Id = x.Id,
                Title = x.Translations.Where(t => t.Culture == culture).Select(t => t.Title).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Title).FirstOrDefault()
                    ?? x.Title,
                Slug = x.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Slug,
                MetaTitle = x.Translations.Where(t => t.Culture == culture).Select(t => t.MetaTitle).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.MetaTitle).FirstOrDefault()
                    ?? x.MetaTitle,
                MetaDescription = x.Translations.Where(t => t.Culture == culture).Select(t => t.MetaDescription).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.MetaDescription).FirstOrDefault()
                    ?? x.MetaDescription
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}

/// <summary>
/// Returns a published CMS page by slug for public delivery.
/// </summary>
public sealed class GetPublishedPageBySlugHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublishedPageBySlugHandler"/> class.
    /// </summary>
    public GetPublishedPageBySlugHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>
    /// Returns a published page for the requested localized slug.
    /// </summary>
    public async Task<PublicPageDetailDto?> HandleAsync(string slug, string culture, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        culture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var defaultCulture = SiteSettingDto.DefaultCultureDefault;
        var normalizedSlug = slug.Trim();
        var nowUtc = DateTime.UtcNow;

        return await _db.Set<Page>()
            .AsNoTracking()
            .Where(x =>
                x.IsPublished &&
                x.Status == PageStatus.Published &&
                (!x.PublishStartUtc.HasValue || x.PublishStartUtc <= nowUtc) &&
                (!x.PublishEndUtc.HasValue || x.PublishEndUtc >= nowUtc) &&
                (x.Slug == normalizedSlug ||
                 x.Translations.Any(t => t.Slug == normalizedSlug && (t.Culture == culture || t.Culture == defaultCulture))))
            .Select(x => new PublicPageDetailDto
            {
                Id = x.Id,
                Title = x.Translations.Where(t => t.Culture == culture).Select(t => t.Title).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Title).FirstOrDefault()
                    ?? x.Title,
                Slug = x.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Slug,
                MetaTitle = x.Translations.Where(t => t.Culture == culture).Select(t => t.MetaTitle).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.MetaTitle).FirstOrDefault()
                    ?? x.MetaTitle,
                MetaDescription = x.Translations.Where(t => t.Culture == culture).Select(t => t.MetaDescription).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.MetaDescription).FirstOrDefault()
                    ?? x.MetaDescription,
                ContentHtml = x.Translations.Where(t => t.Culture == culture).Select(t => t.ContentHtml).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.ContentHtml).FirstOrDefault()
                    ?? x.ContentHtml
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Returns a localized public menu by name.
/// </summary>
public sealed class GetPublicMenuByNameHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublicMenuByNameHandler"/> class.
    /// </summary>
    public GetPublicMenuByNameHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>
    /// Returns the localized public menu for the requested menu name.
    /// </summary>
    public async Task<PublicMenuDto?> HandleAsync(string name, string culture, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        culture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var defaultCulture = SiteSettingDto.DefaultCultureDefault;
        var normalizedName = name.Trim();

        return await _db.Set<Menu>()
            .AsNoTracking()
            .Where(x => x.Name == normalizedName)
            .Select(x => new PublicMenuDto
            {
                Id = x.Id,
                Name = x.Name,
                Items = x.Items
                    .Where(item => item.IsActive)
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new PublicMenuItemDto
                    {
                        Id = item.Id,
                        ParentId = item.ParentId,
                        Label = item.Translations.Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault()
                            ?? item.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Label).FirstOrDefault()
                            ?? item.Title,
                        Url = item.Translations.Where(t => t.Culture == culture).Select(t => t.Url).FirstOrDefault()
                            ?? item.Translations.Where(t => t.Culture == defaultCulture).Select(t => t.Url).FirstOrDefault()
                            ?? item.Url,
                        SortOrder = item.SortOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

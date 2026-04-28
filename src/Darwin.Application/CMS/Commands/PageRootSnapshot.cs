using System.Linq;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;

namespace Darwin.Application.CMS.Commands;

internal static class PageRootSnapshot
{
    public static void SyncFromPrimaryTranslation(Page page)
    {
        var primaryTranslation = page.Translations
            .FirstOrDefault(t => t.Culture == SiteSettingDto.DefaultCultureDefault)
            ?? page.Translations.FirstOrDefault();

        if (primaryTranslation is not null)
        {
            page.Title = primaryTranslation.Title.Trim();
            page.Slug = primaryTranslation.Slug.Trim();
            page.ContentHtml = primaryTranslation.ContentHtml;
            page.MetaTitle = string.IsNullOrWhiteSpace(primaryTranslation.MetaTitle)
                ? primaryTranslation.Title.Trim()
                : primaryTranslation.MetaTitle.Trim();
            page.MetaDescription = primaryTranslation.MetaDescription?.Trim() ?? string.Empty;
        }

        page.IsPublished = page.Status == PageStatus.Published;
    }
}

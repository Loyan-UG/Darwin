using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;


namespace Darwin.Domain.Entities.CMS
{
    /// <summary>
    /// Represents a CMS page with translatable content and SEO metadata.
    /// </summary>
    public sealed class Page : BaseEntity
    {
        /// <summary>Publishing status of the page.</summary>
        public PageStatus Status { get; set; } = PageStatus.Draft;
        /// <summary>Optional publish window start timestamp (UTC).</summary>
        public DateTime? PublishStartUtc { get; set; }
        /// <summary>Optional publish window end timestamp (UTC).</summary>
        public DateTime? PublishEndUtc { get; set; }
        /// <summary>Editor.js raw JSON blocks; stored to allow subsequent edits.</summary>
        public string ContentJson { get; set; } = "{}";
        /// <summary>Pre-rendered and sanitized HTML for fast display and SEO.</summary>
        public string ContentHtml { get; set; } = string.Empty;
        /// <summary>Per-culture translations for title/slug/meta.</summary>
        public List<PageTranslation> Translations { get; set; } = new();
    }


    /// <summary>
    /// Per-culture translation for a CMS page, including SEO metadata and routing slug.
    /// </summary>
    public sealed class PageTranslation : TranslationBase
    {
        /// <summary>Foreign key to the owning page.</summary>
        public Guid PageId { get; set; }
        /// <summary>Localized page title shown in headings and titles.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>URL-friendly slug unique within the culture, used for routing.</summary>
        public string Slug { get; set; } = string.Empty;
        /// <summary>Optional custom HTML title for SEO if different from Title.</summary>
        public string? MetaTitle { get; set; }
        /// <summary>Optional meta description for SEO.</summary>
        public string? MetaDescription { get; set; }
    }


    /// <summary>
    /// Translation entity base for culture-specific content.
    /// </summary>
    public abstract class TranslationBase : BaseEntity
    {
        /// <summary>
        /// Culture identifier for this translation, e.g., "de-DE".
        /// </summary>
        public string Culture { get; set; } = "de-DE";
    }
}
using System;
using System.Collections.Generic;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Catalog
{
    /// <summary>
    /// Category for catalog navigation with per-culture names/slugs.
    /// </summary>
    public sealed class Category : BaseEntity
    {
        /// <summary>Optional parent for building a category tree.</summary>
        public Guid? ParentId { get; set; }
        /// <summary>Sort order among siblings (lower first).</summary>
        public int SortOrder { get; set; }
        /// <summary>Activation flag to control visibility.</summary>
        public bool IsActive { get; set; } = true;
        /// <summary>Per-culture translations for name/slug/description/SEO.</summary>
        public List<CategoryTranslation> Translations { get; set; } = new();
    }


    /// <summary>
    /// Per-culture translation for Category including SEO fields.
    /// </summary>
    public sealed class CategoryTranslation : CMS.TranslationBase
    {
        public Guid CategoryId { get; set; }
        /// <summary>Localized display name of the category.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>URL slug unique within the culture.</summary>
        public string Slug { get; set; } = string.Empty;
        /// <summary>Optional localized description text (may contain sanitized HTML).</summary>
        public string? Description { get; set; }
        /// <summary>Optional SEO meta title override.</summary>
        public string? MetaTitle { get; set; }
        /// <summary>Optional SEO meta description.</summary>
        public string? MetaDescription { get; set; }
    }
}
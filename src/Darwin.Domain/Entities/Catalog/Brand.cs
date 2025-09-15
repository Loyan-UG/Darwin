using System;
using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Catalog
{
    /// <summary>
    ///     Represents a product brand/manufacturer used for attribution, filtering, and brand landing pages.
    ///     This aggregate supports multilingual fields via <see cref="BrandTranslation"/>:
    ///     <list type="bullet">
    ///         <item><c>Name</c> (per-culture): human-readable brand name shown across the UI.</item>
    ///         <item><c>DescriptionHtml</c> (per-culture): optional sanitized HTML for brand description/about content.</item>
    ///     </list>
    ///     Non-translatable fields remain invariant across cultures:
    ///     <list type="bullet">
    ///         <item><c>Slug</c>: SEO-friendly unique identifier for brand landing pages (culture-invariant).</item>
    ///         <item><c>LogoMediaId</c>: optional media identifier for the brand logo.</item>
    ///     </list>
    ///     Auditing, soft-delete, and optimistic concurrency are inherited from <see cref="BaseEntity"/>.
    /// </summary>
    public sealed class Brand : BaseEntity
    {
        /// <summary>
        ///     Optional SEO-friendly unique slug (culture-invariant) used for brand landing routing.
        ///     Keep globally unique if multiple cultures share the same landing page.
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        ///     Optional identifier of the brand logo in the media library (if integrated). Kept as a GUID
        ///     to avoid a hard foreign key dependency while allowing future relation to a media entity.
        /// </summary>
        public Guid? LogoMediaId { get; set; }

        /// <summary>
        ///     Per-culture translations for brand-facing fields (Name, DescriptionHtml).
        /// </summary>
        public List<BrandTranslation> Translations { get; set; } = new();
    }

    /// <summary>
    ///     Culture-specific fields for <see cref="Brand"/> including the display name and optional HTML description.
    ///     Each (BrandId, Culture) pair must be unique to prevent duplicate translations per culture.
    /// </summary>
    public sealed class BrandTranslation : BaseEntity
    {
        /// <summary>
        ///     Owning brand identifier.
        /// </summary>
        public Guid BrandId { get; set; }

        /// <summary>
        ///     Culture code (IETF BCP 47), e.g., "de-DE" or "en-US". Used to resolve localized brand fields.
        /// </summary>
        public string Culture { get; set; } = "de-DE";

        /// <summary>
        ///     Localized brand name shown in UI, lists, and product cards.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Optional localized description/about content for brand landing pages (sanitized HTML).
        ///     Stored as HTML; sanitize on write (Application layer) before persistence.
        /// </summary>
        public string? DescriptionHtml { get; set; }
    }
}

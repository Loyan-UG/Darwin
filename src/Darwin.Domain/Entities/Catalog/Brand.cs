using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Catalog
{
    /// <summary>
    /// Brand/manufacturer record for attribution and filtering.
    /// </summary>
    public sealed class Brand : BaseEntity
    {
        /// <summary>Public brand name displayed on product pages.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Optional SEO-friendly slug for brand landing pages.</summary>
        public string? Slug { get; set; }
        /// <summary>Optional brand logo media id.</summary>
        public System.Guid? LogoMediaId { get; set; }
        /// <summary>Optional description or about text (sanitized HTML).</summary>
        public string? DescriptionHtml { get; set; }
    }
}
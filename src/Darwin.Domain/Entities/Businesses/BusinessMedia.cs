using Darwin.Domain.Common;
using System;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Media item attached to a business or a specific location.
    /// Useful for logos, cover images, and gallery photos in discovery pages.
    /// </summary>
    public sealed class BusinessMedia : BaseEntity
    {
        /// <summary>
        /// FK to the owning business.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Optional FK to a specific location. When null, media is business-wide (e.g., logo).
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Absolute or app-relative URL to the media file.
        /// File storage integration (local/Cloud) is handled in infrastructure.
        /// </summary>
        public string Url { get; set; } = default!;

        /// <summary>
        /// Short, human-readable caption or alt text for accessibility and SEO.
        /// </summary>
        public string? Caption { get; set; }

        /// <summary>
        /// Sort order for galleries; lower numbers appear first.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Whether the file is the primary logo/cover for the business.
        /// </summary>
        public bool IsPrimary { get; set; } = false;
    }
}

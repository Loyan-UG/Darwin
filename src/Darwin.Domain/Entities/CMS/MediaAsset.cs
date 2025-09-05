using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.CMS
{
    /// <summary>
    /// Media asset (image/video). Phase 1 focuses on images with SEO-friendly naming and alt text.
    /// </summary>
    public sealed class MediaAsset : BaseEntity
    {
        /// <summary>Public or relative URL where the media file is served from.</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>Short textual alternative for accessibility and SEO.</summary>
        public string Alt { get; set; } = string.Empty;
        /// <summary>Optional descriptive title attribute.</summary>
        public string? Title { get; set; }
        /// <summary>Original file name at upload time.</summary>
        public string OriginalFileName { get; set; } = string.Empty;
        /// <summary>File size in bytes.</summary>
        public long SizeBytes { get; set; }
        /// <summary>Optional hash (e.g., SHA256) for deduplication or cache busting.</summary>
        public string? ContentHash { get; set; }
        /// <summary>Pixel width if known.</summary>
        public int? Width { get; set; }
        /// <summary>Pixel height if known.</summary>
        public int? Height { get; set; }
        /// <summary>Classifies the role of the media (e.g., Primary, Gallery).</summary>
        public string? Role { get; set; }
    }
}
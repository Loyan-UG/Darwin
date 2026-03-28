using System;

namespace Darwin.Application.CMS.Media.DTOs
{
    public enum MediaAssetQueueFilter
    {
        All = 0,
        MissingAlt = 1,
        EditorAssets = 2,
        LibraryAssets = 3,
        MissingTitle = 4
    }

    /// <summary>
    /// Create payload for a media asset. In phase 1, the file is already stored and we persist metadata only.
    /// </summary>
    public sealed class MediaAssetCreateDto
    {
        public string Url { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? ContentHash { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? Role { get; set; }
    }

    /// <summary>
    /// Edit payload for a media asset. Update descriptive metadata only; the file location remains unchanged.
    /// </summary>
    public sealed class MediaAssetEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string Alt { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Role { get; set; }
    }

    /// <summary>
    /// List item for Admin grid.
    /// </summary>
    public sealed class MediaAssetListItemDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? Role { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class MediaAssetOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int MissingAltCount { get; set; }
        public int MissingTitleCount { get; set; }
        public int EditorAssetCount { get; set; }
        public int LibraryAssetCount { get; set; }
    }

    /// <summary>
    /// Edit projection for a media asset in the Admin portal.
    /// </summary>
    public sealed class MediaAssetEditItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Url { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? Role { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }
}

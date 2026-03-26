using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Darwin.WebAdmin.ViewModels.CMS
{
    /// <summary>
    /// List item view model for the media library.
    /// </summary>
    public sealed class MediaAssetListItemVm
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

    /// <summary>
    /// Paged media-library screen model.
    /// </summary>
    public sealed class MediaAssetsListVm
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 24;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public List<MediaAssetListItemVm> Items { get; set; } = new();
    }

    /// <summary>
    /// Base view model for media editor screens.
    /// </summary>
    public abstract class MediaAssetEditorVm
    {
        public string Alt { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Role { get; set; }
    }

    /// <summary>
    /// Create/upload screen model for a media asset.
    /// </summary>
    public sealed class MediaAssetCreateVm : MediaAssetEditorVm
    {
        public IFormFile? File { get; set; }
    }

    /// <summary>
    /// Edit screen model for a media asset.
    /// </summary>
    public sealed class MediaAssetEditVm : MediaAssetEditorVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Url { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }
}

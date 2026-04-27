using System;
using System.Collections.Generic;
using Darwin.Application.CMS.Media.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        public int ProductReferenceCount { get; set; }
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
        public MediaAssetQueueFilter Filter { get; set; } = MediaAssetQueueFilter.All;
        public MediaAssetOpsSummaryVm Summary { get; set; } = new();
        public List<MediaAssetPlaybookVm> Playbooks { get; set; } = new();
        public List<MediaAssetListItemVm> Items { get; set; } = new();
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class MediaAssetOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int MissingAltCount { get; set; }
        public int MissingTitleCount { get; set; }
        public int EditorAssetCount { get; set; }
        public int LibraryAssetCount { get; set; }
        public int ProductReferencedCount { get; set; }
        public int UnusedCount { get; set; }
    }

    public sealed class MediaAssetPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
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

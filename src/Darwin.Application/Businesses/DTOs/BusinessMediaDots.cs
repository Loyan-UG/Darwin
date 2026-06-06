using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// DTO for attaching media to a business or location.
    /// Hard-managed entity; no soft-delete.
    /// </summary>
    public sealed class BusinessMediaCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public string Url { get; set; } = default!;
        public string? Caption { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
    }

    public sealed class BusinessMediaItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class BusinessMediaLibraryDto
    {
        public Guid BusinessId { get; set; }
        public string? ProfileImageUrl { get; set; }
        public List<BusinessMediaItemDto> Gallery { get; set; } = new();
    }

    public sealed class BusinessProfileImageEditDto
    {
        public Guid BusinessId { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// DTO for editing business media.
    /// </summary>
    public sealed class BusinessMediaEditDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public string Url { get; set; } = default!;
        public string? Caption { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO for hard deleting media.
    /// </summary>
    public sealed class BusinessMediaDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

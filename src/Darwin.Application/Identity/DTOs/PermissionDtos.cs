using System;

namespace Darwin.Application.Identity.DTOs
{
    public sealed class PermissionListItemDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;           // e.g., "Admin.FullAccess"
        public string DisplayName { get; set; } = string.Empty;   // Human-friendly
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
    }

    public sealed class PermissionCreateDto
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; } = false;
    }

    public sealed class PermissionEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

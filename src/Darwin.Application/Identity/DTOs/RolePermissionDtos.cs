using System;
using System.Collections.Generic;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>
    /// Read model for editing a role's permissions in the Admin UI.
    /// Contains the role identity, concurrency token, currently assigned permission ids
    /// and the full selectable permission list for rendering a checklist.
    /// </summary>
    public sealed class RolePermissionsEditDto
    {
        public Guid RoleId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        /// <summary>Human-friendly role name for the header (optional; controller can fill from RoleEditDto).</summary>
        public string RoleDisplayName { get; set; } = string.Empty;

        /// <summary>Currently assigned permission ids to the role (non-deleted links only).</summary>
        public List<Guid> PermissionIds { get; set; } = new();

        /// <summary>Full selectable list for the UI (non-deleted permissions).</summary>
        public List<PermissionListItemDto> AllPermissions { get; set; } = new();
    }

    /// <summary>
    /// Command payload to update a role's permission assignments in one shot.
    /// </summary>
    public sealed class RolePermissionsUpdateDto
    {
        public Guid RoleId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<Guid> PermissionIds { get; set; } = new();
    }
}

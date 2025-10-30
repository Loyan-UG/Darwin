using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Identity
{
    /// <summary>
    /// VM for the "Edit Permissions of a Role" page:
    /// Displays the role summary and a checklist of permissions.
    /// </summary>
    public sealed class RolePermissionsEditVm
    {
        public Guid RoleId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Human-friendly role name for the header (optional; controller can fill from RoleEditDto).</summary>
        public string RoleDisplayName { get; set; } = string.Empty;

        /// <summary>Currently selected permissions.</summary>
        public List<Guid> SelectedPermissionIds { get; set; } = new();

        /// <summary>All selectable permissions for the checklist.</summary>
        public List<PermissionItemVm> AllPermissions { get; set; } = new();
    }


    /// <summary>
    /// Lightweight VM item to render a single permission checkbox row.
    /// </summary>
    public sealed class PermissionItemVm
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;               // Must not be edited if IsSystem
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // Useful for delete actions
    }
}

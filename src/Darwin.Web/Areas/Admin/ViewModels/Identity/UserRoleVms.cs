using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Identity
{
    /// <summary>
    /// VM for the "Edit Roles of a User" page.
    /// Shows the user summary and a checklist of roles.
    /// </summary>
    public sealed class UserRolesEditVm
    {
        public Guid UserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>User display name/email for header.</summary>
        public string UserDisplay { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>Selected roles for this user.</summary>
        public List<Guid> SelectedRoleIds { get; set; } = new();

        /// <summary>All selectable roles to render as checklist.</summary>
        public List<RoleItemVm> AllRoles { get; set; } = new();
    }


    /// <summary>Lightweight VM for a role checkbox row.</summary>
    public sealed class RoleItemVm
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Fine-grained permission primitive. Roles aggregate permissions.
    /// Examples: FullAdminAccess, ManageUsers, ManageRoles, RecycleBinAccess, AccessAdminPanel, AccessMemberArea.
    /// </summary>
    public sealed class Permission : BaseEntity
    {
        /// <summary>System-protected permissions cannot be deleted.</summary>
        public bool IsSystem { get; set; }

        /// <summary>Stable unique key used in code/policies (e.g., "FullAdminAccess").</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Human-friendly name for Admin UI.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Optional long description for documentation.</summary>
        public string? Description { get; set; }

        // Navigations
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        private Permission() { }
    }
}

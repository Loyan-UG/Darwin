using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Authorization role. Aggregates permissions via RolePermission join.
    /// <para>
    /// Name = stable system key (e.g., "admin"), DisplayName = human-friendly caption shown in Admin.
    /// </para>
    /// </summary>
    public sealed class Role : BaseEntity
    {
        /// <summary>System-protected roles cannot be deleted.</summary>
        public bool IsSystem { get; set; }

        /// <summary>Stable technical key (unique). Maps to DTO.Key.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Upper-cased/normalized key for lookups.</summary>
        public string NormalizedName { get; set; } = string.Empty;

        /// <summary>Human-friendly display name shown in Admin. Maps to DTO.DisplayName.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Optional description of the role's purpose.</summary>
        public string? Description { get; set; }

        // Navigations
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        private Role() { }
    }
}

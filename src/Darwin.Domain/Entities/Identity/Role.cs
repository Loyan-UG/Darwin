using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Authorization role. Aggregates permissions via RolePermission.
    /// </summary>
    public sealed class Role : BaseEntity
    {
        /// <summary>System-critical role that cannot be deleted/renamed.</summary>
        public bool IsSystem { get; set; }

        /// <summary>Technical key (unique), e.g., "admin".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Normalized key (usually UPPER).</summary>
        public string NormalizedName { get; set; } = string.Empty;

        /// <summary>Human-friendly display name, e.g., "Administrators".</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Optional description.</summary>
        public string? Description { get; set; }

        // Navigations
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        private Role() { } // EF

        /// <summary>
        /// Preferred factory-style constructor for consistency from Application layer.
        /// </summary>
        public Role(string key, string displayName, bool isSystem, string? description)
        {
            Name = key.Trim();
            NormalizedName = Name.ToUpperInvariant();
            DisplayName = displayName?.Trim() ?? string.Empty;
            IsSystem = isSystem;
            Description = description;
        }
    }
}

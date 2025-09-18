using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Authorization role. Aggregates permissions via RolePermission join.
    /// </summary>
    public sealed class Role : BaseEntity
    {
        public bool IsSystem { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigations
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        private Role() { }
    }
}

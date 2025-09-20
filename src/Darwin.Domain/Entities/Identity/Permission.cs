using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Atomic permission switch (assigned to roles).
    /// </summary>
    public sealed class Permission : BaseEntity
    {
        /// <summary>Permission key (unique, kebab/snake), e.g., "admin.full-access".</summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>Human-friendly name.</summary>
        public string DisplayName { get; private set; } = string.Empty;

        /// <summary>Longer description for admin UIs.</summary>
        public string? Description { get; private set; }

        /// <summary>System flag (non-deletable).</summary>
        public bool IsSystem { get; private set; }

        private Permission() { } // EF

        public Permission(string key, string displayName, bool isSystem, string? description)
        {
            Key = key.Trim();
            DisplayName = displayName?.Trim() ?? string.Empty;
            IsSystem = isSystem;
            Description = description;
        }
    }
}

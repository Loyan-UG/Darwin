using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Join entity assigning a Role to a User. Keeping navigations allows convenient graph traversal in Admin.
    /// </summary>
    public sealed class UserRole : BaseEntity
    {
        public Guid UserId { get; private set; }
        public Guid RoleId { get; private set; }

        // Navigations (optional for EF, useful for Include/fix-up)
        public User? User { get; private set; }
        public Role? Role { get; private set; }

        // EF needs a parameterless constructor
        private UserRole() { }

        public UserRole(Guid userId, Guid roleId)
        {
            UserId = userId;
            RoleId = roleId;
        }
    }
}

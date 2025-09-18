using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Join entity assigning a Permission to a Role.
    /// </summary>
    public sealed class RolePermission : BaseEntity
    {
        public Guid RoleId { get; private set; }
        public Guid PermissionId { get; private set; }

        public Role? Role { get; private set; }
        public Permission? Permission { get; private set; }

        private RolePermission() { }

        public RolePermission(Guid roleId, Guid permissionId)
        {
            RoleId = roleId;
            PermissionId = permissionId;
        }
    }
}

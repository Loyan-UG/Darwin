using System;
using System.Collections.Generic;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>
    /// Read model for editing a user's roles in the Admin UI.
    /// Contains user identity, concurrency token, current role ids and the full selectable role list.
    /// </summary>
    public sealed class UserRolesEditDto
    {
        public Guid UserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public List<Guid> RoleIds { get; set; } = new();
        public List<RoleListItemDto> AllRoles { get; set; } = new();
    }

    /// <summary>
    /// Command payload to update a user's role assignments in one shot.
    /// </summary>
    public sealed class UserRolesUpdateDto
    {
        public Guid UserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<Guid> RoleIds { get; set; } = new();
    }
}

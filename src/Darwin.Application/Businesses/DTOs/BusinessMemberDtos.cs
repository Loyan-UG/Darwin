using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// DTO for linking a user to a business (member).
    /// Hard-managed entity; no soft-delete.
    /// </summary>
    public sealed class BusinessMemberCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for editing a business member.
    /// </summary>
    public sealed class BusinessMemberEditDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; } = true;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO for hard deleting a business member.
    /// </summary>
    public sealed class BusinessMemberDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

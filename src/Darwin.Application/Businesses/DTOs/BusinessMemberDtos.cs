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

    /// <summary>
    /// Lightweight member row for admin paging grids.
    /// </summary>
    public sealed class BusinessMemberListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Richer edit projection for business-member management screens.
    /// </summary>
    public sealed class BusinessMemberDetailDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

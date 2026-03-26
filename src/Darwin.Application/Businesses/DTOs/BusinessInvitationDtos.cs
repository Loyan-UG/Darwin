using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// DTO for issuing a new business invitation.
    /// </summary>
    public sealed class BusinessInvitationCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Email { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public int ExpiresInDays { get; set; } = 7;
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO for invitation resend/reissue flows.
    /// </summary>
    public sealed class BusinessInvitationResendDto
    {
        public Guid Id { get; set; }
        public int ExpiresInDays { get; set; } = 7;
    }

    /// <summary>
    /// DTO for revoking an existing business invitation.
    /// </summary>
    public sealed class BusinessInvitationRevokeDto
    {
        public Guid Id { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Lightweight list row for admin invitation grids.
    /// </summary>
    public sealed class BusinessInvitationListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Email { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public BusinessInvitationStatus Status { get; set; } = BusinessInvitationStatus.Pending;
        public string InvitedByDisplayName { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? AcceptedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Note { get; set; }
    }
}

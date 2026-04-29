using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Optional queue filters for business-invitation support screens.
    /// </summary>
    public enum BusinessInvitationQueueFilter
    {
        All = 0,
        Open = 1,
        Pending = 2,
        Expired = 3,
        Accepted = 4,
        Revoked = 5
    }

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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO for revoking an existing business invitation.
    /// </summary>
    public sealed class BusinessInvitationRevokeDto
    {
        public Guid Id { get; set; }
        public string? Note { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO used by unauthenticated clients to preview an invitation before acceptance.
    /// </summary>
    public sealed class BusinessInvitationPreviewDto
    {
        public Guid InvitationId { get; set; }
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public bool HasExistingUser { get; set; }
    }

    /// <summary>
    /// DTO used when an invited operator accepts a business invitation.
    /// </summary>
    public sealed class BusinessInvitationAcceptDto
    {
        public string Token { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
    }

    /// <summary>
    /// Result DTO returned after successful invitation acceptance.
    /// It contains the authenticated token pair so mobile onboarding can continue without a second login step.
    /// </summary>
    public sealed class BusinessInvitationAcceptanceDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public bool IsNewUser { get; set; }
    }
}

using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents an invitation to join a business workspace.
    /// Used by Business app onboarding flows (invite staff/manager/owner).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Token security:
    /// The raw token can be stored encrypted at rest by Infrastructure (recommended),
    /// similar to other token storage patterns in the solution.
    /// </para>
    /// <para>
    /// Email normalization:
    /// Store both Email and NormalizedEmail to simplify lookups and uniqueness.
    /// Normalization strategy is application-defined (commonly upper-casing).
    /// </para>
    /// </remarks>
    public sealed class BusinessInvitation : BaseEntity
    {
        /// <summary>
        /// Business to which the user is being invited.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// User who created the invitation.
        /// </summary>
        public Guid InvitedByUserId { get; set; }

        /// <summary>
        /// Target email address for the invitation.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Normalized version of <see cref="Email"/> for stable comparisons.
        /// </summary>
        public string NormalizedEmail { get; set; } = string.Empty;

        /// <summary>
        /// Intended role in the business upon acceptance.
        /// </summary>
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;

        /// <summary>
        /// Opaque invitation token (sent to user via email or other channels).
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// When the invitation expires (UTC). Expired invitations cannot be accepted.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// Current status of the invitation.
        /// </summary>
        public BusinessInvitationStatus Status { get; set; } = BusinessInvitationStatus.Pending;

        /// <summary>
        /// When the invitation was accepted (UTC), if accepted.
        /// </summary>
        public DateTime? AcceptedAtUtc { get; set; }

        /// <summary>
        /// The user who accepted the invitation (if acceptance requires authentication).
        /// </summary>
        public Guid? AcceptedByUserId { get; set; }

        /// <summary>
        /// When the invitation was revoked by an admin/owner (UTC), if revoked.
        /// </summary>
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>
        /// Optional internal note for audit/debugging (not shown to end users).
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Marks the invitation as accepted.
        /// </summary>
        public void MarkAccepted(Guid acceptedByUserId, DateTime utcNow)
        {
            if (acceptedByUserId == Guid.Empty) throw new ArgumentException("AcceptedByUserId cannot be empty.", nameof(acceptedByUserId));

            Status = BusinessInvitationStatus.Accepted;
            AcceptedByUserId = acceptedByUserId;
            AcceptedAtUtc = utcNow;
        }

        /// <summary>
        /// Marks the invitation as revoked.
        /// </summary>
        public void Revoke(DateTime utcNow, string? note)
        {
            Status = BusinessInvitationStatus.Revoked;
            RevokedAtUtc = utcNow;
            Note = note;
        }

        /// <summary>
        /// Marks the invitation as expired (typically computed/maintained by a job or query).
        /// </summary>
        public void MarkExpired()
        {
            if (Status == BusinessInvitationStatus.Pending)
                Status = BusinessInvitationStatus.Expired;
        }
    }
}

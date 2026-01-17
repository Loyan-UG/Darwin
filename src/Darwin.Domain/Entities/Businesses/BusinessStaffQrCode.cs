using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents a short-lived QR code token used by business staff flows.
    /// Example use-cases: staff sign-in, terminal pairing, privileged scan actions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is intentionally separate from consumer <c>QrCodeToken</c> to keep loyalty session semantics isolated.
    /// </para>
    /// <para>
    /// Token security:
    /// Store the raw token encrypted at rest in Infrastructure, and keep TTL short.
    /// </para>
    /// </remarks>
    public sealed class BusinessStaffQrCode : BaseEntity
    {
        /// <summary>
        /// Owning business.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Business member for whom the token is issued.
        /// </summary>
        public Guid BusinessMemberId { get; set; }

        /// <summary>
        /// Purpose of the staff QR code.
        /// </summary>
        public BusinessStaffQrPurpose Purpose { get; set; } = BusinessStaffQrPurpose.StaffSignIn;

        /// <summary>
        /// Random opaque token value. This is the only value placed into the QR payload.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// UTC instant when the token was issued.
        /// </summary>
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC expiry for the token. Must be short-lived to minimize replay risk.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// When not null, the token has been consumed and is no longer valid.
        /// </summary>
        public DateTime? ConsumedAtUtc { get; set; }

        /// <summary>
        /// When not null, the token was revoked by an admin and is no longer valid.
        /// </summary>
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>
        /// Optional device id that issued the token (terminal/staff phone).
        /// </summary>
        public string? IssuedDeviceId { get; set; }

        /// <summary>
        /// Optional device id that consumed the token.
        /// </summary>
        public string? ConsumedDeviceId { get; set; }
    }
}

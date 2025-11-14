using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Ephemeral, opaque token issued to the consumer app for presentation as a QR code.
    /// The token contains no PII and no internal identifiers. It is short-lived and one-time use.
    /// </summary>
    public sealed class QrCodeToken : BaseEntity
    {
        /// <summary>
        /// The consumer user that this token is issued to.
        /// Not exposed in the QR payload.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The logical account targeted for accrual/redemption scope.
        /// </summary>
        public Guid? LoyaltyAccountId { get; set; }

        /// <summary>
        /// Random opaque token value (Base64Url or similar). This is the ONLY field that goes into the QR payload.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Purpose of the token such as "Accrual", "Redemption", or "IdentityOnly".
        /// </summary>
        public QrTokenPurpose Purpose { get; set; } = QrTokenPurpose.Accrual;

        /// <summary>
        /// UTC instant when the token was issued.
        /// </summary>
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC expiry. Keep short (e.g., 60-120s) to minimize replay risk.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// When not null, the token has been consumed and is no longer valid.
        /// </summary>
        public DateTime? ConsumedAtUtc { get; set; }

        /// <summary>
        /// Optional mobile device installation identifier to bind token issuance to device.
        /// </summary>
        public string? IssuedDeviceId { get; set; }

        /// <summary>
        /// Optional business that consumed the token (set at scan processing).
        /// </summary>
        public Guid? ConsumedByBusinessId { get; set; }

        /// <summary>
        /// Optional business location that consumed the token.
        /// </summary>
        public Guid? ConsumedByBusinessLocationId { get; set; }
    }
}

using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Arbitrary token storage (email confirm, 2FA recovery, etc.) with expiration.
    /// </summary>
    public sealed class UserToken : BaseEntity
    {
        public Guid UserId { get; set; }

        /// <summary>Purpose discriminator, e.g., "EmailConfirm", "TwoFactorRecovery".</summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>Opaque value (prefer hashed at-rest if sensitive).</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>Optional expiration timestamp.</summary>
        public DateTime? ExpiresAtUtc { get; set; }

        /// <summary>Whether the token was consumed.</summary>
        public DateTime? UsedAtUtc { get; set; }


        private UserToken() { }

        public UserToken(Guid userId, string purpose, string value, DateTime? expiresAtUtc = null)
        {
            UserId = userId;
            Purpose = purpose;
            Value = value;
            ExpiresAtUtc = expiresAtUtc;
        }

        public void MarkUsed(DateTime utcNow) => UsedAtUtc = utcNow;
    }
}

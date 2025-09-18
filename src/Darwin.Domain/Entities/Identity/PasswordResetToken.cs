using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// One-time password reset token for self-service password recovery.
    /// Typically short-lived (e.g., 30–60 minutes).
    /// </summary>
    public sealed class PasswordResetToken : BaseEntity
    {
        public Guid UserId { get; set; }

        /// <summary>Opaque token string (store hashed in DB if you want zero-knowledge).</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Expiration timestamp in UTC.</summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>When the token was consumed; null if not used.</summary>
        public DateTime? UsedAtUtc { get; set; }

        /// <summary>For audit: IP or UA when requested (optional, sanitized).</summary>
        public string? RequestedByMeta { get; set; }


        public User? User { get; private set; }

        private PasswordResetToken() { }

        public PasswordResetToken(Guid userId, string token, DateTime expiresAtUtc, string? requestedByMeta = null)
        {
            UserId = userId;
            Token = token;
            ExpiresAtUtc = expiresAtUtc;
            RequestedByMeta = requestedByMeta;
        }

        public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
        public void MarkUsed(DateTime utcNow) => UsedAtUtc = utcNow;
    }
}

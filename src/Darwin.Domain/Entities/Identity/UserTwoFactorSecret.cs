using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Stores TOTP secret and metadata for two-factor authentication.
    /// Keep secrets encrypted at-rest.
    /// </summary>
    public sealed class UserTwoFactorSecret : BaseEntity
    {
        public Guid UserId { get; set; }

        /// <summary>Base32-encoded secret for TOTP (encrypt at-rest).</summary>
        public string SecretBase32 { get; set; } = string.Empty;

        /// <summary>Issuer label shown in authenticator apps.</summary>
        public string Issuer { get; set; } = "Darwin";

        /// <summary>Account label shown in authenticator apps (often email).</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>When the secret was verified/activated.</summary>
        public DateTime? ActivatedAtUtc { get; set; }


        public User? User { get; private set; }

        private UserTwoFactorSecret() { }

        public UserTwoFactorSecret(Guid userId, string secretBase32, string label, string issuer = "Darwin")
        {
            UserId = userId;
            SecretBase32 = secretBase32;
            Label = label;
            Issuer = issuer;
        }

        public void Activate(DateTime utcNow) => ActivatedAtUtc = utcNow;
    }
}

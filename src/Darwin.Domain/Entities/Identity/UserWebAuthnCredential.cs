using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// A registered WebAuthn (Passkey) credential bound to a user.
    /// Stores the credential id, public key material, AAGUID, and signature counter.
    /// </summary>
    public sealed class UserWebAuthnCredential : BaseEntity
    {
        /// <summary>Owner user id.</summary>
        public Guid UserId { get; set; }

        /// <summary>Credential id (binary, base64url in client). Must be unique per tenant.</summary>
        public byte[] CredentialId { get; set; } = Array.Empty<byte>();

        /// <summary>COSE public key blob (as stored by fido2-net-lib).</summary>
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();

        /// <summary>Authenticator's AAGUID, if provided.</summary>
        public Guid? AaGuid { get; set; }

        /// <summary>WebAuthn credential type, e.g., "public-key".</summary>
        public string CredentialType { get; set; } = "public-key";

        /// <summary>Attestation format (e.g., "packed", "none") if you want to keep it for audit.</summary>
        public string? AttestationFormat { get; set; }

        /// <summary>Signature counter for anti-replay.</summary>
        public uint SignatureCounter { get; set; }

        /// <summary>Optional user handle echoed by authenticator (if provided).</summary>
        public byte[]? UserHandle { get; set; }

        /// <summary>When the credential was last used to sign-in.</summary>
        public DateTime? LastUsedAtUtc { get; set; }

        /// <summary>Heuristic: synced passkey (multi-device) vs device-bound key.</summary>
        public bool IsSyncedPasskey { get; set; }
    }
}

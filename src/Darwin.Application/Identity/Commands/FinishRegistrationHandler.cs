using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Verifies the attestation response and stores the WebAuthn credential for the user.
    /// </summary>
    public sealed class FinishRegistrationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;

        public FinishRegistrationHandler(IAppDbContext db, IWebAuthnService webauthn)
        {
            _db = db; _webauthn = webauthn;
        }

        /// <summary>
        /// Validates the client's attestation response against the stored challenge token,
        /// and on success persists the new credential (enforcing uniqueness).
        /// </summary>
        public async Task<Result> HandleAsync(WebAuthnFinishRegisterDto dto, CancellationToken ct = default)
        {
            var token = await _db.Set<UserToken>().FirstOrDefaultAsync(
                t => t.Id == dto.ChallengeTokenId && t.Purpose == "WebAuthn:Register:Challenge" && t.UsedAtUtc == null && !t.IsDeleted, ct);

            if (token == null || (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc < DateTime.UtcNow))
                return Result.Fail("Challenge expired or not found.");

            var challenge = Convert.FromBase64String(token.Value);
            var (ok, credId, pubKey, aaguid, credType, attFmt, signCount, isSynced, error) =
                await _webauthn.FinishRegistrationAsync(dto.ClientResponseJson, challenge, ct);

            if (!ok) return Result.Fail(error ?? "WebAuthn registration failed.");

            // Enforce uniqueness of CredentialId
            var exists = await _db.Set<UserWebAuthnCredential>().AnyAsync(c => c.CredentialId == credId && !c.IsDeleted, ct);
            if (exists) return Result.Fail("Credential already registered.");

            var userId = token.UserId; // owner from token
            var cred = new UserWebAuthnCredential
            {
                UserId = userId,
                CredentialId = credId,
                PublicKey = pubKey,
                Aaguid = aaguid,
                CredentialType = credType,
                AttestationFormat = attFmt,
                SignCount = signCount,
                IsSyncedPasskey = isSynced
            };

            _db.Set<UserWebAuthnCredential>().Add(cred);
            token.MarkUsed(DateTime.UtcNow);
            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}

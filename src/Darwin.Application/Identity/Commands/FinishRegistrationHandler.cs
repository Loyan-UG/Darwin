using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Verifies the WebAuthn attestation using the original options JSON retrieved
    /// via ChallengeTokenId. On success, persists/updates the user's credential.
    /// </summary>
    public sealed class FinishRegistrationHandler
    {
        private const string Purpose = "WebAuthnRegisterOptions";

        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;

        public FinishRegistrationHandler(IAppDbContext db, IWebAuthnService webauthn)
        {
            _db = db;
            _webauthn = webauthn;
        }

        /// <summary>
        /// Completes the registration by validating attestation and saving the credential.
        /// </summary>
        /// <param name="dto">Challenge token id and raw client response JSON (attestation).</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<Guid>> HandleAsync(WebAuthnFinishRegisterDto dto, CancellationToken ct = default)
        {
            var token = await _db.Set<UserToken>()
                .FirstOrDefaultAsync(t => t.Id == dto.ChallengeTokenId
                                          && t.Purpose == Purpose
                                          && t.UsedAtUtc == null
                                          && (t.ExpiresAtUtc == null || t.ExpiresAtUtc > DateTime.UtcNow), ct);

            if (token is null)
                return Result<Guid>.Fail("Registration session expired or missing.");

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == token.UserId && !u.IsDeleted, ct);
            if (user is null)
                return Result<Guid>.Fail("User not found for this session.");

            var verified = await _webauthn.FinishRegistrationAsync(dto.ClientResponseJson, token.Value, ct);
            if (!verified.Ok)
                return Result<Guid>.Fail(verified.Error ?? "Verification failed.");

            var existing = await _db.Set<UserWebAuthnCredential>()
                .FirstOrDefaultAsync(c => c.CredentialId == verified.CredentialId && !c.IsDeleted, ct);

            if (existing is null)
            {
                var cred = new UserWebAuthnCredential
                {
                    UserId = user.Id,
                    CredentialId = verified.CredentialId,
                    PublicKey = verified.PublicKey,
                    AaGuid = verified.Aaguid,
                    CredentialType = verified.CredType,
                    AttestationFormat = verified.AttestationFmt,
                    SignatureCounter = verified.SignCount,
                    LastUsedAtUtc = DateTime.UtcNow,
                    IsSyncedPasskey = verified.IsSynced
                };
                _db.Set<UserWebAuthnCredential>().Add(cred);
            }
            else
            {
                existing.PublicKey = verified.PublicKey;
                existing.AaGuid = verified.Aaguid;
                existing.SignatureCounter = verified.SignCount;
                existing.LastUsedAtUtc = DateTime.UtcNow;
            }

            token.MarkUsed(DateTime.UtcNow);
            await _db.SaveChangesAsync(ct);

            return Result<Guid>.Ok(user.Id);
        }
    }
}

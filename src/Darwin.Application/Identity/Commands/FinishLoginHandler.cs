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
    /// Completes a WebAuthn login by verifying the assertion and updating the credential's signature counter.
    /// </summary>
    public sealed class FinishLoginHandler
    {
        private const string Purpose = "WebAuthnLoginOptions";

        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;

        public FinishLoginHandler(IAppDbContext db, IWebAuthnService webauthn)
        {
            _db = db;
            _webauthn = webauthn;
        }

        /// <summary>
        /// Verifies the assertion using the original options JSON retrieved by ChallengeTokenId.
        /// </summary>
        /// <param name="dto">Challenge token id and the raw assertion response JSON.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result> HandleAsync(WebAuthnFinishLoginDto dto, CancellationToken ct = default)
        {
            var token = await _db.Set<UserToken>()
                .FirstOrDefaultAsync(t => t.Id == dto.ChallengeTokenId
                                          && t.Purpose == Purpose
                                          && t.UsedAtUtc == null
                                          && (t.ExpiresAtUtc == null || t.ExpiresAtUtc > DateTime.UtcNow), ct);

            if (token is null)
                return Result.Fail("Login session expired or missing.");

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == token.UserId && !u.IsDeleted, ct);
            if (user is null)
                return Result.Fail("User not found for this session.");

            var verified = await _webauthn.FinishLoginAsync(dto.ClientResponseJson, token.Value, ct);
            if (!verified.Ok)
                return Result.Fail(verified.Error ?? "Verification failed.");

            // Update counter and last-used
            var cred = await _db.Set<UserWebAuthnCredential>()
                .FirstOrDefaultAsync(c => c.CredentialId == verified.CredentialId && !c.IsDeleted, ct);

            if (cred is null)
                return Result.Fail("Credential not found after verification.");

            cred.SignatureCounter = verified.NewSignCount;
            cred.LastUsedAtUtc = DateTime.UtcNow;

            token.MarkUsed(DateTime.UtcNow);
            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}

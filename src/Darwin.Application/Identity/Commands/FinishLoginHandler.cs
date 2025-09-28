using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Verifies the assertion response, updates the sign counter, and marks the challenge token used.
    /// </summary>
    public sealed class FinishLoginHandler
    {
        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;

        public FinishLoginHandler(IAppDbContext db, IWebAuthnService webauthn)
        {
            _db = db; _webauthn = webauthn;
        }

        /// <summary>
        /// Validates the client's assertion against the stored challenge and updates the credential's sign count.
        /// </summary>
        public async Task<Result> HandleAsync(WebAuthnFinishLoginDto dto, CancellationToken ct = default)
        {
            var token = await _db.Set<UserToken>().FirstOrDefaultAsync(
                t => t.Id == dto.ChallengeTokenId && t.Purpose == "WebAuthn:Login:Challenge" && t.UsedAtUtc == null && !t.IsDeleted, ct);

            if (token == null || (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc < DateTime.UtcNow))
                return Result.Fail("Challenge expired or not found.");

            var challenge = Convert.FromBase64String(token.Value);
            var (ok, credId, newSignCount, error) = await _webauthn.FinishLoginAsync(dto.ClientResponseJson, challenge, ct);
            if (!ok) return Result.Fail(error ?? "WebAuthn assertion failed.");

            var cred = await _db.Set<UserWebAuthnCredential>().FirstOrDefaultAsync(
                c => c.UserId == token.UserId && c.CredentialId == credId && !c.IsDeleted, ct);

            if (cred == null) return Result.Fail("Credential not recognized.");

            cred.SignCount = newSignCount;
            cred.LastUsedAtUtc = DateTime.UtcNow;

            token.MarkUsed(DateTime.UtcNow);
            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}

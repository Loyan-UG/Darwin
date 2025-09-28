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
    /// Starts WebAuthn login by generating an assertion challenge for a user's registered credentials.
    /// </summary>
    public sealed class BeginLoginHandler
    {
        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;

        public BeginLoginHandler(IAppDbContext db, IWebAuthnService webauthn)
        {
            _db = db; _webauthn = webauthn;
        }

        /// <summary>
        /// Collects allowed credential IDs for the user and returns a request options JSON + persisted challenge token.
        /// </summary>
        public async Task<Result<WebAuthnBeginLoginResult>> HandleAsync(WebAuthnBeginLoginDto dto, CancellationToken ct = default)
        {
            var creds = await _db.Set<UserWebAuthnCredential>()
                .AsNoTracking()
                .Where(c => c.UserId == dto.UserId && !c.IsDeleted)
                .Select(c => c.CredentialId)
                .ToListAsync(ct);

            var (optionsJson, challenge) = await _webauthn.BeginLoginAsync(dto.UserId, creds, ct);

            var token = new UserToken(
                userId: dto.UserId,
                purpose: "WebAuthn:Login:Challenge",
                value: Convert.ToBase64String(challenge),
                expiresAtUtc: DateTime.UtcNow.AddMinutes(10)
            );

            _db.Set<UserToken>().Add(token);
            await _db.SaveChangesAsync(ct);

            return Result<WebAuthnBeginLoginResult>.Ok(new WebAuthnBeginLoginResult
            {
                ChallengeTokenId = token.Id,
                OptionsJson = optionsJson
            });
        }
    }
}
